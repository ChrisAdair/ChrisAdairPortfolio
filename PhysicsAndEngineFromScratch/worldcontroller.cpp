#include "worldcontroller.h"
#include <osg/Node>
#include <osg/Geode>
#include <osg/Material>
#include <osg/Shape>
#include <osg/ShapeDrawable>
#include <osg/MatrixTransform>
#include <osg/PrimitiveSet>
#include <osg/PositionAttitudeTransform>

WorldController::WorldController(osg::ref_ptr<osg::Group> baseNode)
{
    mRoot = baseNode;
    aimDirection.set(1,0,0);
    score=0;
}

void WorldController::UpdatePhysics(float deltaTime)
{
    for(int i=0;i<(int)listeners.size();i++)
    {
        listeners[i]->UpdatePhysics(deltaTime);
    }
    CalculateCollisions();
}

void WorldController::CalculateCollisions()
{
    for(int i=0; i<(int)listeners.size()-1;i++)
    {
        for(int j=i+1; j<(int)listeners.size();j++)
        {
            if(listeners[i]->physics->collider->hasCollided(listeners[j]->physics->collider))
            {
                listeners[i]->physics->resolve_collision(listeners[j]->physics);
                listeners[j]->physics->resolve_collision(listeners[i]->physics);
                if(listeners[i]->isScoreTrigger || listeners[j]->isScoreTrigger)
                    ScorePoints();
            }
        }
    }
}
void WorldController::ScorePoints()
{
    score += 10;
}
void WorldController::AddUpdateListener(gameObject *listener)
{
    listeners.push_back(listener);
}

void WorldController::CreateSphere()
{
    osg::Sphere* sphere    = new osg::Sphere( osg::Vec3( 0.f, 0.f, 0.f ), 1.0f );
    osg::ShapeDrawable* sd = new osg::ShapeDrawable( sphere );
    sd->setColor( osg::Vec4( 1.f, 0.f, 0.f, 1.f ) );
    sd->setName( "Sphere" );

    osg::Geode* geode = new osg::Geode;
    geode->addDrawable( sd );

    osg::ref_ptr<osg::MatrixTransform> sphereTrans = new osg::MatrixTransform;
    osg::Matrix m;
    m.makeTranslate(0,0,3);
    sphereTrans->setMatrix(m);
    sphereTrans->addChild(geode);
    osg::StateSet* stateSet = geode->getOrCreateStateSet();
    osg::Material* material = new osg::Material;

    material->setColorMode( osg::Material::AMBIENT_AND_DIFFUSE );

    stateSet->setAttributeAndModes( material, osg::StateAttribute::ON );
    stateSet->setMode( GL_DEPTH_TEST, osg::StateAttribute::ON );
    gameObject *sphereGameObject = new gameObject();
    sphereGameObject->Set_Sphere_Collider(1);
    sphereGameObject->physics->acceleration = osg::Vec3f(0,0,-9.81f);
    sphereGameObject->physics->position = osg::Vec3f(0,0,3);
    aimDirection.normalize();
    aimDirection *=10;
    sphereGameObject->physics->velocity.set(aimDirection.x(),aimDirection.y(),aimDirection.z());
    sphereTrans->setUpdateCallback(sphereGameObject);
    mRoot->addChild(sphereTrans);

    AddUpdateListener(sphereGameObject);
}
void WorldController::CreateTarget()
{
    osg::Sphere* sphere    = new osg::Sphere( osg::Vec3( 0.f, 0.f, 0.f ), 1.0f );
    osg::ShapeDrawable* sd = new osg::ShapeDrawable( sphere );
    sd->setColor( osg::Vec4( 0.f, 0.f, 1.f, 1.f ) );
    sd->setName( "Sphere" );

    osg::Geode* geode = new osg::Geode;
    geode->addDrawable( sd );

    osg::ref_ptr<osg::MatrixTransform> sphereTrans = new osg::MatrixTransform;
    osg::Matrix m;
    m.makeTranslate(0,8,3);
    sphereTrans->setMatrix(m);
    sphereTrans->addChild(geode);
    osg::StateSet* stateSet = geode->getOrCreateStateSet();
    osg::Material* material = new osg::Material;

    material->setColorMode( osg::Material::AMBIENT_AND_DIFFUSE );

    stateSet->setAttributeAndModes( material, osg::StateAttribute::ON );
    stateSet->setMode( GL_DEPTH_TEST, osg::StateAttribute::ON );
    gameObject *sphereGameObject = new gameObject();
    sphereGameObject->Set_Sphere_Collider(1);
    sphereGameObject->physics->position = osg::Vec3f(0,8,3);
    sphereTrans->setUpdateCallback(sphereGameObject);
    mRoot->addChild(sphereTrans);

    AddUpdateListener(sphereGameObject);
}
void WorldController::CreateBoundingBox()
{
    osg::Vec3Array* v = new osg::Vec3Array;
        v->resize( 4 );
        (*v)[0].set( 1.f, 0.f, -.707f );
        (*v)[1].set(-1.f, 0.f, -.707f );
        (*v)[2].set(0.f, 1.f, .707f );
        (*v)[3].set(0.f, -1.f, .707f );

        osg::Geometry* geom = new osg::Geometry;
        geom->setUseDisplayList( false );
        geom->setVertexArray( v );

        osg::Vec4Array* c = new osg::Vec4Array;
        c->push_back({1,1,1,1}  );
        geom->setColorArray( c, osg::Array::BIND_OVERALL );

        GLushort idxLines[6] = {0, 3, 1, 3, 2, 3};
        GLushort idxLoops[3] = {0, 1, 2 };

        geom->addPrimitiveSet( new osg::DrawElementsUShort( osg::PrimitiveSet::LINES, 6, idxLines ) );
        geom->addPrimitiveSet( new osg::DrawElementsUShort( osg::PrimitiveSet::LINE_LOOP, 3, idxLoops ) );

        osg::Geode* geode = new osg::Geode;
        geode->addDrawable( geom );

        geode->getOrCreateStateSet()->setMode( GL_LIGHTING, osg::StateAttribute::OFF | osg::StateAttribute::PROTECTED );
        geode->getOrCreateStateSet()->setMode( GL_DEPTH_TEST, osg::StateAttribute::ON );
        osg::PositionAttitudeTransform* transform = new osg::PositionAttitudeTransform;
        transform->setScale({10,10,10});
        transform->addChild(geode);
        mRoot->addChild(transform);
}

void WorldController::CreateWireframePlane(osg::Vec3f *points)
{
    osg::Geode *plane = new osg::Geode();
    osg::Geometry *geometry = new osg::Geometry();
    osg::Vec3Array *pointArray = new osg::Vec3Array(4, points);
    geometry->setVertexArray(pointArray);
    osg::Vec4Array* colors = new osg::Vec4Array;
    colors->push_back(osg::Vec4(1.0f,1.0f,1.0f,1.0f));
    geometry->setColorArray(colors);
    geometry->setColorBinding(osg::Geometry::BIND_OVERALL);
    GLushort line_loop[4] = {0,1,2,3};
    geometry->addPrimitiveSet(new osg::DrawElementsUShort(osg::PrimitiveSet::QUADS, 4, line_loop));
    plane->getOrCreateStateSet()->setMode( GL_LIGHTING, osg::StateAttribute::OFF | osg::StateAttribute::PROTECTED );
    plane->getOrCreateStateSet()->setMode( GL_DEPTH_TEST, osg::StateAttribute::ON );
    plane->addDrawable(geometry);
    gameObject *planeCallbacks = new gameObject();
    planeCallbacks->Set_Plane_Collider(points, 0.8f);
    plane->addDrawable(geometry);
    osg::ref_ptr<osg::MatrixTransform> planeTrans = new osg::MatrixTransform;
    osg::Matrix m;
    m.makeTranslate(0,0,0);
    planeTrans->setMatrix(m);
    planeTrans->addChild(plane);
    planeTrans->setUpdateCallback(planeCallbacks);
    AddUpdateListener(planeCallbacks);
    mRoot->addChild(planeTrans);
}
