#ifndef COLLIDER_H
#define COLLIDER_H

#include <osg/Vec3f>

class Collider
{
public:
    enum colliderType
    {
        sphere,
        plane
    };
    Collider();
    Collider(colliderType type, osg::Vec3f bounds[]);

    float radius;
    osg::Vec3f planeBounds[3];
    float coefficientOfRestitution;
    osg::Vec3f position;
    osg::Vec3f normal;

    colliderType type;

    bool hasCollided(Collider *other);
    bool SphereVsSphere(Collider *other);
    bool SphereVsPlane(Collider *other);
    bool PlaneVsSphere(Collider *other);


    float EuclidianDistance(osg::Vec3f first, osg::Vec3f second);
    float PointPlaneDistance(osg::Vec3f point, osg::Vec3f planeCorners[3]);
    bool OutOfBounds(osg::Vec3f point, osg::Vec3f planeCorners[3]);
    void EvaluateNormal();
};

#endif // COLLIDER_H
