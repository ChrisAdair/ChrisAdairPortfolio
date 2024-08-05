#include "collider.h"

Collider::Collider()
{

}
Collider::Collider(colliderType type, osg::Vec3f bounds[])
{
    if(type == plane)
    {
        for(int i=0; i<3;i++)
        {
            planeBounds[i] = bounds[i];
        }
        EvaluateNormal();
    }
}
void Collider::EvaluateNormal()
{
    normal.set( (planeBounds[1]-planeBounds[0])^(planeBounds[2]-planeBounds[1]));
    normal.normalize();
}
bool Collider::hasCollided(Collider *other)
{

    switch(this->type)
    {
    case sphere:
        if(other->type == sphere)
            return SphereVsSphere(other);
        else
            return SphereVsPlane(other);
    case plane:
        if(other->type == sphere)
            return PlaneVsSphere(other);
        break;
    }
    return false;
}

bool Collider::PlaneVsSphere(Collider *other)
{
    if(std::abs(this->PointPlaneDistance(other->position,this->planeBounds)) < other->radius)
        return true;
    else
        return false;
}

bool Collider::SphereVsPlane(Collider *other)
{
    if(std::abs(this->PointPlaneDistance(this->position,other->planeBounds)) < this->radius)
        return true;
    else
        return false;
}

bool Collider::SphereVsSphere(Collider *other)
{
    if(this->EuclidianDistance(this->position,other->position) < this->radius + other->radius)
        return true;
    else
        return false;
}
float Collider::EuclidianDistance(osg::Vec3f first, osg::Vec3f second)
{
    return std::sqrtf((second-first)*(second-first));
}

float Collider::PointPlaneDistance(osg::Vec3f point, osg::Vec3f planeCorners[3])
{
    osg::Vec3f unitNormal;
    unitNormal.set( (planeCorners[1]-planeCorners[0])^(planeCorners[2]-planeCorners[1]));
    unitNormal.normalize();
    return unitNormal*(point - planeCorners[0]);
}

bool Collider::OutOfBounds(osg::Vec3f point, osg::Vec3f planeCorners[3])
{
    return false;
}
