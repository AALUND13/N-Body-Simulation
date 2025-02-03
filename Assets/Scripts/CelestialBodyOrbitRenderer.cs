//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//[ExecuteInEditMode]
//[RequireComponent(typeof(CelestialBody))]
//public class CelestialBodyOrbitRenderer : MonoBehaviour {
//    //private LineRenderer lineRenderer;
//    private CelestialBody celestialBody;
//    public CelestialBody RelevantCelestialBody;
//    public int steps = 100;

//    private void OnEnable() {
//        celestialBody = GetComponent<CelestialBody>();
//        celestialBody.OnChangeEvent.AddListener(OnPropertyChange);
//    }

//    private void OnDisable() {
//        celestialBody.OnChangeEvent.RemoveListener(OnPropertyChange);
//    }

//    private void OnPropertyChange() {
//        foreach(CelestialBody celestialBody in CelestialBodyManager.instance.celestialBodies) {
//            if(!CelestialBodyManager.instance.positionsDict.ContainsKey(celestialBody)) return;
//            CelestialBodyManager.instance.positionsDict[celestialBody].Item1.Clear();
//            CelestialBodyManager.instance.positionsDict[celestialBody].Item2.Clear();
//        }
//    }

//    private void OnDrawGizmos() {
//        if(celestialBody == null || Application.isPlaying) return;
//        var celestialBodies = CelestialBodyManager.instance.SimulateStep(steps);
//        Gizmos.color = Color.green;
//        List<Vector3> positions = new List<Vector3>(celestialBodies[celestialBody]);
//        for(int i = 0; i < positions.Count; i++) {
//            if(RelevantCelestialBody != null) {
//                positions[i] -= celestialBodies[RelevantCelestialBody][i];
//            }
//        }
//        Gizmos.DrawLineStrip(positions.ToArray(), false);
//    }
//}
