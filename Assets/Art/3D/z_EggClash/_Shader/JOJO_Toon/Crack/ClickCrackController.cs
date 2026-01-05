using UnityEngine;
using System.Collections.Generic;

public class ClickCrackController : MonoBehaviour
{
    [Header("Settings")]
    public Material crackMaterial;   // Shore “Crack Decals” 쉐이더가 할당된 머티리얼
    public int      maxCracks = 8;
    public float    crackLifetime = 10f;

    private List<Vector4> _cracks = new List<Vector4>();
    private float         _startTime;
    private Transform     _rendererTransform;

    void Start()
    {
        _startTime = Time.time;
        _rendererTransform = GetComponentInChildren<SkinnedMeshRenderer>()?.transform
                          ?? GetComponent<MeshRenderer>().transform;
        // 머티리얼 인스턴스 만들어서 적용
        crackMaterial = Instantiate(crackMaterial);
        _rendererTransform.GetComponent<Renderer>().material = crackMaterial;
        crackMaterial.SetFloat("_EnableCrackDecals", 1f);
    }

    void OnCollisionEnter(Collision col)
    {
        // 플레이어끼리만, 혹은 원하는 레이어필터 추가
        // if (!col.collider.CompareTag("Player")) return;
        if (col.relativeVelocity.magnitude < 0.5f) return;

        // 첫 접촉점 하나만 사용
        var pt = col.contacts[0].point;

        // 월드→로컬 좌표로 변환
        Vector3 local = _rendererTransform.InverseTransformPoint(pt);
        AddCrack(new Vector4(local.x, local.y, local.z, 1f));
    }

    void AddCrack(Vector4 localPos)
    {
        // 만약 초과하면 앞에서 제거
        if (_cracks.Count >= maxCracks)
            _cracks.RemoveAt(0);

        _cracks.Add(localPos);
        UpdateShaderCracks();
        // 만료 코루틴
        StartCoroutine(RemoveAfter(crackLifetime));
    }

    System.Collections.IEnumerator RemoveAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (_cracks.Count > 0)
        {
            _cracks.RemoveAt(0);
            UpdateShaderCracks();
        }
    }

    void UpdateShaderCracks()
    {
        crackMaterial.SetFloat("_ActiveCracks", _cracks.Count);
        for (int i = 0; i < maxCracks; i++)
        {
            var v = i < _cracks.Count ? _cracks[i] : Vector4.zero;
            crackMaterial.SetVector($"_ClickPos{i+1}", v);
        }
    }
}
