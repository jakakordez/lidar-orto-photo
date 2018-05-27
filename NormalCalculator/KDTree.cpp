//
// Created by peter on 7.12.2017.
//

#include "stdafx.h"
#include <algorithm>
#include "KDTree.h"

typedef std::vector<const Point3d*>::iterator pIter;

KDTree::KDTree(const Point3d& p, int const axis) : p(p), axis(axis) {}

KDTree::~KDTree() {
    delete left;
    delete right;
}

KDTree* KDTree::createTree(const std::vector<Point3d>& pointsOrig) {
    size_t const n = pointsOrig.size();
    std::vector<const Point3d*> points(n);
    for (size_t i = 0; i < n; i++) {
        points[i] = &pointsOrig[i];
    }
    return createTree(points);
}

KDTree* KDTree::createTree(const std::vector<Point3d*>& pointsOrig) {
    size_t const n = pointsOrig.size();
    std::vector<const Point3d*> points(n);
    for (size_t i = 0; i < n; ++i) {
        points[i] = pointsOrig[i];
    }
    return createTree(points);
}

KDTree* KDTree::createTree(std::vector<const Point3d*>& points) {
    return createTree(points, points.begin(), points.end(), 0);
}

KDTree* KDTree::createTree(std::vector<const Point3d*>& points,
                           const pIter& start,
                           const pIter& end,
                           int const depth) {
    // start is inclusive, end is not

    long n = end - start;
    if (n <= 0) {
        return nullptr;
    }

    int const axis = depth % k;
    std::sort(start, end, Point3d::comparators[axis]);
    pIter const median = start + n/2;

    KDTree* const newNode = new KDTree(**median, axis);
    newNode->left  = createTree(points, start, median, depth + 1);
    newNode->right = createTree(points, median+1, end, depth + 1);
    return newNode;
}

std::vector<const Point3d*> KDTree::getNeighbors(const Point3d& target, double const radius) const {
    std::vector<const Point3d*> neighbors;
    getNeighbors(neighbors, target, radius);
    return neighbors;
}

void KDTree::getNeighbors(std::vector<const Point3d*>& neighbors, const Point3d& target, double const radius) const {
    double const thisPos = p.coord(axis);
    double const targetPos = target.coord(axis);

    bool const  leftOfTarget = thisPos <= targetPos + radius;
    bool const rightOfTarget = thisPos >= targetPos - radius;
    if (leftOfTarget && right != nullptr) {
        right->getNeighbors(neighbors, target, radius);
    }
    if (rightOfTarget && left != nullptr) {
        left->getNeighbors(neighbors, target, radius);
    }
    if (leftOfTarget && rightOfTarget && &p != &target && p.distSqTo(target) <= radius*radius) {
        neighbors.push_back(&p);
    }
}
