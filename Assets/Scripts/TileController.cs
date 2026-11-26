using UnityEngine;

public class TileController : MonoBehaviour
{
    [SerializeField] private int _index;
    [SerializeField] private float _hoverHeight = 0.2f;
    [SerializeField] private float _hoverSpeed = 8f;
    [SerializeField] private float _signHeight = 0.05f;

    [SerializeField] private GameObject _crossPrefab;
    [SerializeField] private GameObject _circlePrefab;

    private bool _hasSign;
    private bool _isHovered;
    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.localPosition;
        GameManager.Instance.OnMarkPlaced += HandleMarkPlaced;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMarkPlaced -= HandleMarkPlaced;
    }

    private void Update()
    {
        Vector3 targetPosition = _startPos;

        if (_isHovered && CanInteract())
            targetPosition += Vector3.up * _hoverHeight;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            _hoverSpeed * Time.deltaTime
        );
    }

    private bool CanInteract()
    {
        return GameManager.Instance != null
            && !_hasSign
            && GameManager.Instance.CanPlace;
    }

    private void OnMouseDown()
    {
        if (!CanInteract())
            return;

        GameManager.Instance.PlaceMark(_index);
    }

    private void OnMouseEnter() => _isHovered = true;
    private void OnMouseExit() => _isHovered = false;

    private void HandleMarkPlaced(int cell, int player)
    {
        if (cell != _index) return;

        _hasSign = true;
        _isHovered = false;
        SpawnSign(player);
    }

    private void SpawnSign(int player)
    {
        GameObject prefab = player == 1 ? _crossPrefab : _circlePrefab;

        Vector3 spawnPosition = transform.parent.TransformPoint(_startPos);
        spawnPosition += Vector3.up * _signHeight;

        Instantiate(prefab, spawnPosition, Quaternion.identity, GameManager.Instance.BoardRootTransform);
    }
}