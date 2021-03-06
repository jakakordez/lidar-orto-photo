// NormalCalculator.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <vector>
#include <cmath>
#include "dsyevh3.h"
#include "Point3d.h"
#include "KDTree.h"
#include "laszip_api.h"

#include <Windows.h>
double get_wall_time() {
	LARGE_INTEGER time, freq;
	if (!QueryPerformanceFrequency(&freq)) {
		//  Handle error
		return 0;
	}
	if (!QueryPerformanceCounter(&time)) {
		//  Handle error
		return 0;
	}
	return (double)time.QuadPart / freq.QuadPart;
}

int main(int argc, char **argv)
{

	printf("Normal calculator\n");
	fflush(stdout);
	
	if (argc < 4) {
		printf("Parameters: PATH X Y\n");
		fflush(stdout);
		return 1;
	}

	char *path = argv[1];
	int x = atoi(argv[2]);
	int y = atoi(argv[3]);

	char startPath[100];
	char endPath[100];
	sprintf_s(startPath, "%s/7-%d-%d.laz", path, x, y);
	sprintf_s(endPath, "%s/8-%d-%d.laz", path, x, y);

	//FILE *a = (FILE *)malloc(sizeof(FILE));
	//fopen_s(&a, "test.txt", "w");

	laszip_POINTER readFile = NULL;
	laszip_POINTER writeFile = NULL;
	laszip_BOOL compressed = 1;
	laszip_create(&readFile);
	laszip_open_reader(readFile, startPath, &compressed);
	laszip_seek_point(readFile, 0);
	
	laszip_create(&writeFile);
	
	laszip_header_struct *header = (laszip_header_struct *)malloc(sizeof(laszip_header_struct));
	laszip_get_header_pointer(readFile, &header);
	laszip_set_header(writeFile, header);

	laszip_open_writer(writeFile, endPath, true);

	laszip_read_point(readFile);
	laszip_F64 coordinates[3];

	long long n = (header->number_of_point_records ? header->number_of_point_records : header->extended_number_of_point_records);

	std::vector<Point3d> points;

	laszip_point* lPoint;
	laszip_get_point_pointer(readFile, &lPoint);

	printf("Reading %d points\n", n);
	fflush(stdout);
	for (int i = 0; i < n; i++) {
		laszip_read_point(readFile);
		points.push_back(Point3d((double)lPoint->X, (double)lPoint->Y, (double)lPoint->Z));
	}
	
	printf("Building KD tree\n");
	fflush(stdout);

	const KDTree* tree = KDTree::createTree(points);
	laszip_point* writePoint = (laszip_point_struct*)malloc(sizeof(laszip_point_struct));
	laszip_point_struct *readPoint = (laszip_point_struct*)malloc(sizeof(laszip_point_struct));

	laszip_get_point_pointer(writeFile, &writePoint);
	laszip_get_point_pointer(readFile, &readPoint);
	printf("Writing LAZ\n");
	fflush(stdout);
	laszip_seek_point(readFile, 0);
	for (int i = 0; i < n; i++) {
		laszip_read_point(readFile);
		
		if (i % 1000000 == 0 && i > 0) {
			printf("%d mio points processed\n", i / 1000000);
			fflush(stdout);
		}

		double v[3];

		if (readPoint->classification == 9) // WATER
		{
			v[0] = 0;
			v[1] = 0;
			v[2] = 1;
		}
		else {
			Point3d p = points[i];
			//std::cout << "[" << p.x << ", " << p.y << "," << p.z << "] ";
			const std::vector<const Point3d*> neighborhood = tree->getNeighbors(p, 500.0);
			//std::cout << neighborhood.size() << " neighbours ";

			double covMatrix[3][3];
			covMatrix[0][0] = covMatrix[0][1] = covMatrix[0][2] = 0.0;
			covMatrix[1][0] = covMatrix[1][1] = covMatrix[1][2] = 0.0;
			covMatrix[2][0] = covMatrix[2][1] = covMatrix[2][2] = 0.0;
			for (int j = 0; j < neighborhood.size(); j++) {
				double dx = neighborhood[j]->x - p.x;
				double dy = neighborhood[j]->y - p.y;
				double dz = neighborhood[j]->z - p.z;

				covMatrix[0][0] += dx*dx;
				covMatrix[0][1] += dx*dy;
				covMatrix[0][2] += dx*dz;
				covMatrix[1][1] += dy*dy;
				covMatrix[1][2] += dy*dz;
				covMatrix[2][2] += dz*dz;
			}
			covMatrix[1][0] = covMatrix[0][1];
			covMatrix[2][0] = covMatrix[0][2];
			covMatrix[2][1] = covMatrix[1][2];

			double Q[3][3];
			double w[3];
			dsyevh3(covMatrix, Q, w);

			int index = 2;
			if (w[0] < w[1] && w[0] < w[2]) index = 0;
			else if (w[1] < w[0] && w[1] < w[2]) index = 1;

			v[0] = Q[0][index];
			v[1] = Q[1][index];
			v[2] = Q[2][index];

			if (v[2] < -0.3) {
				v[0] = -v[0];
				v[1] = -v[1];
				v[2] = -v[2];
			}
		}

		//laszip_set_point(writePoint, )
		
		readPoint->rgb[0] |= (uint8_t)(v[0] * 128);
		readPoint->rgb[1] |= (uint8_t)(v[1] * 128);
		readPoint->rgb[2] |= (uint8_t)(v[2] * 128);

		//memcpy_s((void *)writePoint, sizeof(laszip_point_struct), (void *)readPoint, sizeof(laszip_point_struct));
		/*writePoint->X = readPoint->X;
		writePoint->Y = readPoint->Y;
		writePoint->Z = readPoint->Z;
		writePoint->intensity = readPoint->intensity;
		writePoint->return_number = readPoint->return_number;
		writePoint->number_of_returns = readPoint->number_of_returns;
		writePoint->scan_direction_flag = readPoint->scan_direction_flag;
		writePoint->edge_of_flight_line = readPoint->edge_of_flight_line;
		writePoint->classification = readPoint->classification;
		writePoint->withheld_flag = readPoint->withheld_flag;
		writePoint->keypoint_flag = readPoint->keypoint_flag;
		writePoint->synthetic_flag = readPoint->synthetic_flag;
		writePoint->scan_angle_rank = readPoint->scan_angle_rank;
		writePoint->user_data = readPoint->user_data;
		writePoint->point_source_ID = readPoint->point_source_ID;

		writePoint->gps_time = readPoint->gps_time;
		memcpy_s(writePoint->rgb, 8, readPoint->rgb, 8);
		memcpy_s(writePoint->wave_packet, 29, readPoint->wave_packet, 29);*/

		laszip_set_point(writeFile, readPoint);
		laszip_write_point(writeFile);

		//std::cout << v[0] << ", " << v[1] << ", " << v[2] << std::endl;
	}
	laszip_close_reader(readFile);
	laszip_close_writer(writeFile);
	printf("Done\n");
	fflush(stdout);
}
