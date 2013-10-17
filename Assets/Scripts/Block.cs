using UnityEngine;


public class Block : MonoBehaviour
{
	public enum Type :byte
	{
		air,
		dirt,
		undefined = 255
	}


	public static bool isTransparent( Block.Type block )
	{
		return (block == Type.air);
	}
}
