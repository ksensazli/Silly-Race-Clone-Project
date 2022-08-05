using DG.Tweening;
using PaintIn3D.Examples;
using UnityEngine;

public class PaintingManager : MonoBehaviour
{
    [SerializeField] private GameObject _brush;
    [SerializeField] private TMPro.TMP_Text _percentageText;
    [SerializeField] private P3dColor _p3dColor;
    [SerializeField] private RectTransform _rect;
    private bool _isPainting;
    void OnEnable()
    {
        GameManager.OnEndReached += paintingStarted;
        _rect.DOAnchorPosX(-100,1f).SetLoops(-1,LoopType.Yoyo);
    }

    void OnDisable() 
    {
        GameManager.OnEndReached -= paintingStarted;
    }

    void Update()
    {
        if(!_isPainting)
        {
            return;
        }
        _percentageText.text = (_p3dColor.Ratio * 100).ToString("f1");

        if(_p3dColor.Ratio >= .995f)
        {
            _percentageText.text = "100";
            GameManager.OnLevelCompleted?.Invoke();
            _isPainting = false;
        }
    }

    private void paintingStarted()
    {
        _brush.SetActive(true);
        _percentageText.gameObject.SetActive(true);
        _isPainting = true;
    }
}
