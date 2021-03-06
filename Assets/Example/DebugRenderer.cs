﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Urg
{
    public class DebugRenderer : MonoBehaviour
    {
        public UrgSensor urg;

        private float[] distances;
        private List<DetectedLocation> locations = new List<DetectedLocation>();
        private AffineConverter affineConverter;
        private List<GameObject> debugObjects;
        private Object syncLock = new Object();

        void Awake()
        {
            // delegate method to receive raw distance data from sensor.
            urg.OnDistanceReceived += Urg_OnDistanceReceived;

            // delegate method to receive filtered detected locations.
            urg.OnLocationDetected += Urg_OnLocationDetected;

            urg.AddFilter(new SpatialMedianFilter(3));
            urg.AddFilter(new ClusteringFilter(0.15f));

            var cam = Camera.main;
            var plane = new Plane(Vector3.up, Vector3.zero);

            var sensorCorners = new Vector2[4];
            sensorCorners[0] = new Vector2(1.5f, 0.5f);
            sensorCorners[1] = new Vector2(1.5f, -0.5f);
            sensorCorners[2] = new Vector2(0.5f, -0.5f);
            sensorCorners[3] = new Vector2(0.5f, 0.5f);

            var worldCorners = new Vector3[4];
            worldCorners[0] = Screen2WorldPosition(new Vector2(0, Screen.height), cam, plane);
            worldCorners[1] = Screen2WorldPosition(new Vector2(Screen.width, Screen.height), cam, plane);
            worldCorners[2] = Screen2WorldPosition(new Vector2(Screen.width, 0), cam, plane);
            worldCorners[3] = Screen2WorldPosition(new Vector2(0, 0), cam, plane);
            affineConverter = new AffineConverter(sensorCorners, worldCorners);

            debugObjects = new List<GameObject>();
            for (var i = 0; i < 100; i++)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.parent = transform;
                obj.transform.localScale = 0.3f * Vector3.one;
                debugObjects.Add(obj);
            }
        }

        void Update()
        {
            if (urg == null)
            {
                return;
            }

            if (distances != null && distances.Length > 0)
            {
                for (int i = 0; i < distances.Length; i++)
                {
                    float distance = distances[i];
                    float angle = urg.StepAngleRadians * i + urg.OffsetRadians;
                    var cos = Mathf.Cos(angle);
                    var sin = Mathf.Sin(angle);
                    var dir = new Vector3(cos, 0, sin);
                    var pos = distance * dir;

                    Debug.DrawRay(urg.transform.position, pos, Color.blue);
                }
            }

            var locs = this.locations;
            int index = 0;
            foreach (var loc in locs)
            {
                Vector3 worldPos = new Vector3(0, 0, 0);
                var inRegion = affineConverter.Sensor2WorldPosition(loc.ToPosition2D(), out worldPos);
                if (inRegion && index < debugObjects.Count)
                {
                    //Gizmos.DrawCube(worldPos, new Vector3(0.1f, 0.1f, 0.1f));
                    debugObjects[index].transform.position = worldPos;
                    index++;
                }
            }

            for (var i = index; i < debugObjects.Count; i++)
            {
                debugObjects[i].transform.position = new Vector3(100, 100, 100);
            }
        }

        void Urg_OnDistanceReceived(float[] rawDistances)
        {
            this.distances = rawDistances;
        }

        void Urg_OnLocationDetected(List<DetectedLocation> locations)
        {
            // this is called outside main thread.
            this.locations = locations;
        }

        private static Vector3 Screen2WorldPosition(Vector2 screenPosition, Camera camera, Plane basePlane)
        {
            var ray = camera.ScreenPointToRay(screenPosition);
            var distance = 0f;

            if (basePlane.Raycast(ray, out distance))
            {
                var p = ray.GetPoint(distance);
                return p;
            }
            return Vector3.negativeInfinity;
        }
    }
}
