using UnityEngine;

public class CameraView : MonoBehaviour
{
    private Camera viewCamera;
    private readonly Plane[] planes = new Plane[6];
    private readonly Ray[] cornerRays = new Ray[4];

    private int cachedFrame = -1;
    private Matrix4x4 viewMatrix;
    private Matrix4x4 projectionMatrix;
    private bool planesReady;
    private bool raysReady;

    public Plane[] FrustumPlanes
    {
        get
        {
            RefreshSnapshot();
            if (!planesReady)
            {
                GeometryUtility.CalculateFrustumPlanes(viewCamera, planes);
                planesReady = true;
            }
            return planes;
        }
    }

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
    }

    public Ray GetCornerRay(int index)
    {
        RefreshSnapshot();

        if (!raysReady)
        {
            for (int i = 0; i < cornerRays.Length; i++)
            {
                cornerRays[i] = viewCamera.ViewportPointToRay(new Vector3(i == 1 || i == 2 ? 1f : 0f, i >= 2 ? 1f : 0f, 0f));
            }

            raysReady = true;
        }

        return cornerRays[index];
    }

    public bool TryGetCorners(float height, Vector3[] corners, out Vector3 center)
    {
        center = Vector3.zero;
        Plane plane = new(Vector3.up, new Vector3(0f, height, 0f));

        for (int i = 0; i < 4; i++)
        {
            Ray ray = GetCornerRay(i);
            if (!plane.Raycast(ray, out float distance)) return false;

            corners[i] = ray.GetPoint(distance);
            center += corners[i] * 0.25f;
        }

        return true;
    }

    private void RefreshSnapshot()
    {
        if (viewCamera == null) viewCamera = GetComponent<Camera>();

        Matrix4x4 currentView = viewCamera.worldToCameraMatrix;
        Matrix4x4 currentProjection = viewCamera.projectionMatrix;

        if (cachedFrame == Time.frameCount && viewMatrix == currentView && projectionMatrix == currentProjection) return;

        cachedFrame = Time.frameCount;
        viewMatrix = currentView;
        projectionMatrix = currentProjection;
        planesReady = false;
        raysReady = false;
    }
}
