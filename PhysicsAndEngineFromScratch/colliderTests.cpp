#include "gtest/gtest.h"
#include "collider.h"
#include <osg/Vec3f>


TEST(ColliderTests, point_plane_distance_correct_calculation)
{
    Collider point;
    point.position = osg::Vec3f(0,0,1);
    osg::Vec3f rightHandRuleBounds[]{osg::Vec3f(0,0,0), osg::Vec3f(0,1,0), osg::Vec3f(1,0,0)};
    Collider plane(Collider::plane, rightHandRuleBounds);
    point.position = osg::Vec3f(0,0,1);
    EXPECT_EQ(-1,point.PointPlaneDistance(point.position,plane.planeBounds));
}

TEST(ColliderTests, euclidian_distance_correct_calculation)
{
    Collider point1;
    Collider point2;
    point1.position = osg::Vec3f(0,0,5);
    point2.position = osg::Vec3f(0,0,0);
    EXPECT_EQ(5,point1.EuclidianDistance(point1.position,point2.position));
}

TEST(ColliderTests, ball_plane_collision_detected)
{
    Collider point;
    point.position = osg::Vec3f(0,0,1);
    osg::Vec3f rightHandRuleBounds[]{osg::Vec3f(0,0,0), osg::Vec3f(0,1,0), osg::Vec3f(1,0,0)};
    Collider *plane = new Collider(Collider::plane, rightHandRuleBounds);
    point.position = osg::Vec3f(0,0,1);
    point.radius = 0.5f;
    EXPECT_FALSE(point.SphereVsPlane(plane));

    point.radius = 2;
    EXPECT_TRUE(point.SphereVsPlane(plane));
    delete plane;
}

TEST(ColliderTests, plane_ball_collision_detected)
{
    Collider *point = new Collider();
    point->position = osg::Vec3f(0,0,1);
    osg::Vec3f rightHandRuleBounds[]{osg::Vec3f(0,0,0), osg::Vec3f(0,1,0), osg::Vec3f(1,0,0)};
    Collider plane(Collider::plane, rightHandRuleBounds);
    point->position = osg::Vec3f(0,0,1);
    point->radius = 0.5f;
    EXPECT_FALSE(plane.PlaneVsSphere(point));

    point->radius = 2;
    EXPECT_TRUE(plane.PlaneVsSphere(point));
    delete point;
}

TEST(ColliderTests, ball_ball_collision_detected)
{
    Collider *point = new Collider();
    point->position = osg::Vec3f(0,0,4);
    point->radius = 1;
    point->type = Collider::sphere;
    Collider *point2 = new Collider();
    point2->position = osg::Vec3f(0,0,0);
    point2->radius = 1;
    point2->type = Collider::sphere;

    EXPECT_FALSE(point->hasCollided(point2));

    point2->radius = 4;

    EXPECT_TRUE(point->hasCollided(point2));
}
