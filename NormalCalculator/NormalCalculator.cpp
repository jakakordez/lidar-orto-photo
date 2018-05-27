// NormalCalculator.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <vector>
#include <cmath>
#include "dsyevh3.h"
#include "kdtree2.hpp"
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

int main()
{
	
	FILE *a = (FILE *)malloc(sizeof(FILE));
	fopen_s(&a, "test.txt", "w");

	laszip_POINTER readFile = 0;
	laszip_BOOL compressed = 1;
	laszip_create(&readFile);
	laszip_open_reader(readFile, "SloveniaLidarRGB0.laz", &compressed);
	laszip_seek_point(readFile, 0);
	
	laszip_header_struct *header = (laszip_header_struct *)malloc(sizeof(laszip_header_struct));
	laszip_get_header_pointer(readFile, &header);

	laszip_read_point(readFile);
	laszip_F64 coordinates[3];

	long long n = (header->number_of_point_records ? header->number_of_point_records : header->extended_number_of_point_records);

	kdtree2::KDTreeArray tree(boost::extents[n][3]);

	laszip_point* lPoint;
	laszip_get_point_pointer(readFile, &lPoint);

	for (int i = 0; i < n; i++) {
		laszip_read_point(readFile);
		tree[i][0] = lPoint->X;
		tree[i][1] = lPoint->Y;
		tree[i][2] = lPoint->Z;
	}
	
	kdtree2::KDTree *arr = new kdtree2::KDTree(tree, true, 3);

	std::vector<float> point;
	point.push_back(4.5f);
	point.push_back(4.5f);
	point.push_back(4.5f);

	kdtree2::KDTreeResultVector result;
	arr->n_nearest(point, 2, result);

	double M[3][3];
	M[0][0] = 13.3999;
	M[0][1] = 13.4013;
	M[0][2] = -6.7499;
	M[1][0] = 13.4013;
	M[1][1] = 29.9939;
	M[1][2] = -4.1334;
	M[2][0] = -6.7499;
	M[2][1] = -4.1334;
	M[2][2] = 3.838;
	double Q[3][3];
	double w[3];
	double s = get_wall_time();
	dsyevh3(M, Q, w);
	double f = get_wall_time();
	double e = f - s;
	
	int index = 2;
	if (w[0] < w[1] && w[0] < w[2]) index = 0;
	else if (w[1] < w[0] && w[1] < w[2]) index = 1;

	double v[3];
	v[0] = Q[0][index];
	v[1] = Q[1][index];
	v[2] = Q[2][index];

	std::cout << v[0] << ", " << v[1] << ", " << v[2];

	/*std::vector<double> eigs = eigenvalues((double[3][3])M);
	std::cout << "Eigenvalues: ";
	std::cout.precision(std::numeric_limits<double>::digits10 + 5);
	std::cout << std::fixed << std::scientific;
	for (auto eig : eigs)
	{
		std::cout << eig << ", ";
	}
	std::cout << '\n';*/
}
