//
// Created by peter on 7.12.2017.
//

#ifndef ORTHOLIDAR_KDTREE_H
#define ORTHOLIDAR_KDTREE_H

#include "stdafx.h"
#include <vector>
#include "Point3d.h"

class KDTree {
private:
    const Point3d& p;
    KDTree* left;
    KDTree* right;
    int axis;

    static int const k = 3;

public:
    KDTree(const Point3d& p, int axis);
    ~KDTree();

    static KDTree* createTree(const std::vector<Point3d>& points);
    static KDTree* createTree(const std::vector<Point3d*>& points);
    static KDTree* createTree(std::vector<const Point3d*>& points);  // reorders elements of given vector

    std::vector<const Point3d*> getNeighbors(const Point3d& target, double radius) const;

private:
    static KDTree* createTree(std::vector<const Point3d*>& points,  // reorders elements of given vector
                              const std::vector<const Point3d*>::iterator& start,
                              const std::vector<const Point3d*>::iterator& end,
                              int depth);

    void getNeighbors (std::vector<const Point3d*>& neighbors, const Point3d& target, double radius) const;
};


#endif //ORTHOLIDAR_KDTREE_H
