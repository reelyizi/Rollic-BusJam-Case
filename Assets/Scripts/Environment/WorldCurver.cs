using UnityEngine;

[ExecuteInEditMode]
public class WorldCurver : MonoBehaviour
{
    [Range(-0.1f, 0.1f)]
    public float curveStrength = 0.01f;

    private int curveStrengthID;

    private void OnEnable()
    {
        curveStrengthID = Shader.PropertyToID("_CurveStrength");
    }

    private void Update()
    {
        Shader.SetGlobalFloat(curveStrengthID, curveStrength);
    }
}
