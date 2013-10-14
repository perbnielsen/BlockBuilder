using UnityEngine;

[RequireComponent( typeof( MeshCollider  ) )]
public class Terrain : MonoBehaviour
{
	public int seed;


	void Start()
	{
		Random.seed = seed;
	}
	
	
	void Update()
	{
	
	}
}
