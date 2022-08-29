using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public enum RotationAxes
    {
        MouseX = 1,
        MouseY = 2
    }

    public RotationAxes axes = RotationAxes.MouseX;
    public float sensitivityX = 15f;
    public float sensitivityY = 15f;
    public float minimumY = -60f;
    public float maximumY = 60f;
    float rotationX = 0f;

    void Update()
    {
        switch (axes)
        {
            case RotationAxes.MouseX:
            {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
                break;
            }
            case RotationAxes.MouseY:
            {
                var rotation = transform.localEulerAngles;
                rotation.x = rotationX = Mathf.Clamp(rotationX - Input.GetAxis("Mouse Y") * sensitivityY, minimumY, maximumY);
                transform.localEulerAngles = rotation;
                break;
            }
        }
    }
}