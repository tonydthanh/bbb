using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceSorter : IComparer {
	public Vector3 fromWhere;
	public int Compare (object x, object y) {
		
		if(x == null) {
			return -1;
		}
		if(y == null) {
			return 1;
		}
		
		float a = (((RaycastHit)x).transform.position - fromWhere).magnitude;
		float b = (((RaycastHit)y).transform.position - fromWhere).magnitude;
		
		if(a < b) {
			return -1;
		}
		if(a > b) {
			return 1;
		}
		return 0;
	}
	
	private static DistanceSorter sortoid = new DistanceSorter();
	
	
	public static void Sort(Vector3 start, Array raycastHits) {
		sortoid.fromWhere=start;
		Array.Sort(raycastHits,sortoid);
	}
}
