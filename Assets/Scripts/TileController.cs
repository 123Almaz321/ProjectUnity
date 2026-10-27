using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class TileController : MonoBehaviour
{
    [SerializeField] private float _hoverHeight = 0.2f;
    [SerializeField] private float _hoverSpeed = 8f;

    private bool _isHovered;
    private Vector3 _startPos;

    private void Start()
    {
        _isHovered = false;
        _startPos = transform.localPosition;
    }

    void Update()
    {
        Vector3 targetPosition = _startPos;

        if (_isHovered)
            targetPosition += Vector3.up * _hoverHeight;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, _hoverSpeed * Time.deltaTime);
    }

    private void OnMouseEnter()
    {
        _isHovered = true;
    }

    private void OnMouseExit()
    {
        _isHovered = false;
    }
}
