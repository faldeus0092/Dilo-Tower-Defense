using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _towerIcon;

    private Tower _currentSpawnedTower;
    private Tower _towerPrefab;

    // ketika awal mendrag

    // Implementasi dari Interface IBeginDragHandler
    // Fungsi ini terpanggil sekali ketika pertama men-drag UI
    public void OnBeginDrag(PointerEventData eventData)
    {
        GameObject newTowerObj = Instantiate(_towerPrefab.gameObject);
        _currentSpawnedTower = newTowerObj.GetComponent<Tower>();
        _currentSpawnedTower.ToggleOrderInLayer(true);
    }


    // proses mendrag
    // Implementasi dari Interface IDragHandler
    // Fungsi ini terpanggil selama men-drag UI
    public void OnDrag(PointerEventData eventData)
    {
        Camera mainCamera = Camera.main;
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        _currentSpawnedTower.transform.position = targetPosition;
    }


    // akhir ngedrag
    // Implementasi dari Interface IEndDragHandler
    // Fungsi ini terpanggil sekali ketika men-drop UI ini
    public void OnEndDrag(PointerEventData eventData)
    {
        // cek apakah bisa ditempati
        if (_currentSpawnedTower.PlacePosition == null)
        {
            Destroy(_currentSpawnedTower.gameObject);
        }
        // cek apakah cost cukup
        else if(LevelManager.Instance._currentDeploymentPoint < _currentSpawnedTower._deploymentCost)
        {
            Destroy(_currentSpawnedTower.gameObject);
        }
        else
        {
            _currentSpawnedTower.LockPlacement();
            _currentSpawnedTower.ToggleOrderInLayer(false);
            LevelManager.Instance.RegisterSpawnedTower(_currentSpawnedTower);
            // Debug.Log(_currentSpawnedTower.PlacePosition);
            _currentSpawnedTower = null;
        }

    }

    public void SetTowerPrefab(Tower tower)
    {
        _towerPrefab = tower;
        _towerIcon.sprite = tower.GetTowerHeadIcon();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
