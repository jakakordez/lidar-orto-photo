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
	if (argc < 4) {
		printf("Parameters: PATH X Y\n");
		return 1;
	}

	char *path = argv[1];
	int x = atoi(argv[2]);
	int y = atoi(argv[3]);

	char startPath[100];
	char endPath[100];
	sprintf_s(startPath, "%s/7-%d-%d.laz", path, x, y);
	sprintf_s(endPath, "%s/8-%d-%d.laz", path, x, y);

	FILE *a = (FILE *)malloc(sizeof(FILE));
	fopen_s(&a, "test.txt", "w");

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

	laszip_read_point(readFile);
	laszip_F64 coordinates[3];

	long long n = (header->number_of_point_records ? header->number_of_point_records : header->extended_number_of_point_records);

	std::vector<Point3d> points;

	laszip_point* lPoint;
	laszip_get_point_pointer(readFile, &lPoint);

	printf("Reading %d points\n", n);
	for (int i = 0; i < n; i++) {
		laszip_read_point(readFile);
		points.push_back(Point3d((double)lPoint->X, (double)lPoint->Y, (double)lPoint->Z));
	}
	printf("Building KD tree\n");

	const KDTree* tree = KDTree::createTree(points);
	laszip_point* writePoint = (laszip_point_struct*)malloc(sizeof(laszip_point_struct));
	laszip_point_struct *readPoint = (laszip_point_struct*)malloc(sizeof(laszip_point_struct));

	laszip_seek_point(readFile, 0);
	for (int i = 0; i < n; i++) {
		Point3d p = points[i];
		//std::cout << "[" << p.x << ", " << p.y << "," << p.z << "] ";
		const std::vector<const Point3d*> neighborhood = tree->getNeighbors(p, 50.0);
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

		double v[3];
		v[0] = Q[0][index];
		v[1] = Q[1][index];
		v[2] = Q[2][index];

		laszip_get_point_pointer(writeFile, &writePoint);
		laszip_get_point_pointer(readFile, &readPoint);

		memcpy_s((void *)writePoint, sizeof(laszip_point_struct), (void *)readPoint, sizeof(laszip_point_struct));

		writePoint->rgb[0] |= (uint8_t)(v[0] * 255) - 128;
		writePoint->rgb[1] |= (uint8_t)(v[1] * 255) - 128;
		writePoint->rgb[2] |= (uint8_t)(v[2] * 255) - 128;

		laszip_write_point(writeFile);

		//std::cout << v[0] << ", " << v[1] << ", " << v[2] << std::endl;
	}
	laszip_close_reader(readFile);
	laszip_close_writer(writeFile);
	printf("Done\n");
}
