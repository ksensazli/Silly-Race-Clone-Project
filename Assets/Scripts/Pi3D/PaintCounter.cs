using PaintIn3D.Examples;
using UnityEngine;

public class PaintCounter : MonoBehaviour
{
    [SerializeField] private P3dColor p3dColor;
    [SerializeField] private TMPro.TMP_Text text;
    
    void Update()
    {
        text.text = (p3dColor.Ratio * 100).ToString("f1");
    }
}
