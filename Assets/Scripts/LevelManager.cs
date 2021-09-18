using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    // Fungsi Singleton
    private static LevelManager _instance = null;

    private List<Tower> _spawnedTowers = new List<Tower>();
    private List<Enemy> _spawnedEnemies = new List<Enemy>();
    private List<Bullet> _spawnedBullets = new List<Bullet>();

    [SerializeField] private int _maxLives = 3;
    [SerializeField] private int _totalEnemy = 15;
    [SerializeField] private Transform _towerUIParent;
    [SerializeField] private GameObject _towerUIPrefab;
    [SerializeField] private Tower[] _towerPrefabs;
    [SerializeField] private Enemy[] _enemyPrefabs;
    [SerializeField] private Transform[] _enemyPaths;
    [SerializeField] private float _spawnDelay = 5f;
    [SerializeField] private float _deploymentPointDelay = 3f;
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _statusInfo;
    [SerializeField] private Text _livesInfo;
    [SerializeField] private Text _totalEnemyInfo;
    [SerializeField] private Text _deploymentPointInfo;

    private float _runningSpawnDelay;
    private float _runningDeploymentPointDelay;
    public bool IsOver { get; private set; }
    private int _currentLives;
    private int _enemyCounter;
    public int _currentDeploymentPoint = 1;

    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelManager>();
            }
            return _instance;
        }
    }

    private void Start()
    {
        SetCurrentLives(_maxLives);
        SetTotalEnemy(_totalEnemy);
        InstantiateAllTowerUI();
    }

    // Menampilkan seluruh Tower yang tersedia pada UI Tower Selection
    private void InstantiateAllTowerUI()
    {
        // untuk setiap tower prefabs yang diserialize ...
        foreach (Tower tower in _towerPrefabs)
        {
            // instantiate
            GameObject newTowerUIObj = Instantiate(_towerUIPrefab.gameObject, _towerUIParent);
            TowerUI newTowerUI = newTowerUIObj.GetComponent<TowerUI>();

            newTowerUI.SetTowerPrefab(tower);
            newTowerUI.transform.name = tower.name;
        }

    }

    // Update is called once per frame
    void Update()
    {
        // Counter untuk spawn enemy dalam jeda waktu yang ditentukan
        // Time.unscaledDeltaTime adalah deltaTime yang independent, tidak terpengaruh oleh apapun kecuali game object itu sendiri,
        // jadi bisa digunakan sebagai penghitung waktu

        _runningSpawnDelay -= Time.unscaledDeltaTime;
        if (_runningSpawnDelay <= 0f)
        {
            SpawnEnemy();
            _runningSpawnDelay = _spawnDelay;
        }

        _runningDeploymentPointDelay -= Time.unscaledDeltaTime;
        if(_runningDeploymentPointDelay <= 0f)
        {
            _currentDeploymentPoint++;
            _deploymentPointInfo.text = $"DP: {Mathf.Max(_currentDeploymentPoint, 0)}";
            _runningDeploymentPointDelay = _deploymentPointDelay;
        }

        foreach (Tower tower in _spawnedTowers)
        {
            tower.CheckNearestEnemy(_spawnedEnemies);
            tower.SeekTarget();
            tower.ShootTarget();
        }

        // Restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }



        if (IsOver)

        {

            return;

        }

        // loop untuk tiap enemy
        foreach (Enemy enemy in _spawnedEnemies)
        {
            if (!enemy.gameObject.activeSelf)
            {
                continue;
            }

            // Kenapa nilainya 0.1? Karena untuk lebih mentoleransi perbedaan posisi,
            // akan terlalu sulit jika perbedaan posisinya harus 0 atau sama persis
            if (Vector2.Distance(enemy.transform.position, enemy.TargetPosition) < 0.1f)
            {
                // pathnya ditambah
                enemy.SetCurrentPathIndex(enemy.CurrentPathIndex + 1);
                // jika belum mencapai length, pindah target
                if (enemy.CurrentPathIndex < _enemyPaths.Length)
                {
                    enemy.SetTargetPosition(_enemyPaths[enemy.CurrentPathIndex].position);
                }
                // jika sudah melewati length, deactive
                else
                {
                    ReduceLives(1);
                    enemy.gameObject.SetActive(false);
                }
            }
            else
            {
                enemy.MoveToTarget();
            }

        }
    }

    // Mendaftarkan Tower yang di-spawn agar bisa dikontrol oleh LevelManager

    public void RegisterSpawnedTower(Tower tower)
    {
        _spawnedTowers.Add(tower);
        _currentDeploymentPoint -= tower._deploymentCost;
    }

    private void SpawnEnemy()
    {
        SetTotalEnemy(--_enemyCounter);
        if (_enemyCounter < 0)
        {
            bool isAllEnemyDestroyed = _spawnedEnemies.Find(e => e.gameObject.activeSelf) == null;
            if (isAllEnemyDestroyed)
            {
                SetGameOver(true);
            }
            return;
        }

        // ngerandom apa ini
        int randomIndex = Random.Range(0, _enemyPrefabs.Length);
        string enemyIndexString = (randomIndex + 1).ToString();

        GameObject newEnemyObj = _spawnedEnemies.Find(
            e => !e.gameObject.activeSelf && e.name.Contains(enemyIndexString)
        )?.gameObject;

        if (newEnemyObj == null)
        {
            newEnemyObj = Instantiate(_enemyPrefabs[randomIndex].gameObject);
        }

        Enemy newEnemy = newEnemyObj.GetComponent<Enemy>();
        if (!_spawnedEnemies.Contains(newEnemy))
        {
            _spawnedEnemies.Add(newEnemy);
        }

        newEnemy.transform.position = _enemyPaths[0].position;
        newEnemy.SetTargetPosition(_enemyPaths[1].position);
        newEnemy.SetCurrentPathIndex(1);
        newEnemy.gameObject.SetActive(true);
    }

    public Bullet GetBulletFromPool(Bullet prefab)
    {
        GameObject newBulletObj = _spawnedBullets.Find(
            b => !b.gameObject.activeSelf && b.name.Contains(prefab.name)
        )?.gameObject;

        if (newBulletObj == null)
        {
            newBulletObj = Instantiate(prefab.gameObject);
        }

        Bullet newBullet = newBulletObj.GetComponent<Bullet>();
        if (!_spawnedBullets.Contains(newBullet))
        {
            _spawnedBullets.Add(newBullet);
        }
        return newBullet;
    }

    public void ExplodeAt(Vector2 point, float radius, int damage)
    {
        foreach (Enemy enemy in _spawnedEnemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                if (Vector2.Distance(enemy.transform.position, point) <= radius)
                {
                    enemy.ReduceEnemyHealth(damage);
                }
            }
        }
    }

    public void ReduceLives(int value)
    {
        SetCurrentLives(_currentLives - value);
        if (_currentLives <= 0)
        {
            SetGameOver(false);
        }
    }

    public void SetCurrentLives(int currentLives)
    {
        // Mathf.Max fungsi nya adalah mengambil angka terbesar
        // sehingga _currentLives di sini tidak akan lebih kecil dari 0
        _currentLives = Mathf.Max(currentLives, 0);
        _livesInfo.text = $"Lives: {_currentLives}";
    }

    public void SetTotalEnemy(int totalEnemy)
    {
        _enemyCounter = totalEnemy;
        _totalEnemyInfo.text = $"Total Enemy: {Mathf.Max(_enemyCounter, 0)}";
    }

    public void SetGameOver(bool isWin)
    {
        IsOver = true;
        _statusInfo.text = isWin ? "You Win!" : "You Lose!";
        _panel.gameObject.SetActive(true);
    }

    // Untuk menampilkan garis penghubung dalam window Scene
    // tanpa harus di-Play terlebih dahulu
    private void OnDrawGizmos()
    {
        for (int i = 0; i < _enemyPaths.Length - 1; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_enemyPaths[i].position, _enemyPaths[i + 1].position);
        }

    }
}
