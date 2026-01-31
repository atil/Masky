using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public enum State
    {
        None,
        Countdown,
        Search,
        Shoot,
        Score,
    }

    public class GameMain : MonoBehaviour
    {
        [SerializeField] private Root _root;
        [SerializeField] private MeshRenderer _theRenderer;
        [SerializeField] private BoxCollider2D _spotVolume;
        [SerializeField] private GameObject _spotPrefab;
        [SerializeField] private GameObject _shotMarkPrefab;
        [SerializeField] private Transform _spotsParent;
        [SerializeField] private Transform _shotMarksParent;
        [SerializeField] private GameObject _searchText;
        [SerializeField] private GameObject _shootText;
        [SerializeField] private GameObject _scoreText;
        [SerializeField] private Color _redColor;
        [SerializeField] private Color _whiteColor;
        [SerializeField] private List<Image> _edgeBarsAll;
        [SerializeField] private Image[] _edgeBarsHorizontal;
        [SerializeField] private Image[] _edgeBarsVertical;

        State _state;
        float _timer = 0;

        int _score = 0;

        const int SpotCount = 5;
        const float SearchDuration = 5.0f;
        const float ShootDuration = 5.0f;

        public void Setup()
        {
            Cursor.visible = false;
        }

        public void ResetGame()
        {
            _state = State.Countdown;
            _theRenderer.material.SetVector("_MousePos", new Vector4(99, 99, 0, 1));

            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            while (true)
            {
                if (_state == State.Countdown)
                {
                    foreach (Transform t in _spotsParent) Destroy(t.gameObject);
                    foreach (Transform t in _shotMarksParent) Destroy(t.gameObject);

                    List<Vector2> spotPositions = new();
                    Vector2 s = _spotVolume.size / 2;
                    for (int i = 0; i < SpotCount; i++)
                    {
                        float x = Random.Range(-s.x, s.x);
                        float y = Random.Range(-s.y, s.y);

                        // TODO prevent overlap

                        spotPositions.Add(new(x, y));
                    }
                    foreach (Vector2 pos in spotPositions)
                    {
                        GameObject spotGo = Instantiate(_spotPrefab, _spotsParent);
                        spotGo.transform.position = pos;
                    }

                    _searchText.SetActive(true);
                    yield return new WaitForSeconds(1.0f);
                    _state = State.Search;
                }
                else if (_state == State.Search)
                {
                    _searchText.SetActive(false);
                    Cursor.visible = false;
                    _edgeBarsAll.ForEach(x => x.color = _whiteColor);
                    _edgeBarsAll.ForEach(x => x.rectTransform.localScale = Vector3.one);

                    while (true)
                    {
                        // TODO UI edge bars
                        Vector3 p = _root.Camera.ScreenToWorldPoint(Input.mousePosition);
                        _theRenderer.material.SetVector("_MousePos", new Vector4(p.x, p.y, 0, 1));

                        _timer += Time.deltaTime;

                        float t = Mathf.Lerp(1, 0.01f, _timer / SearchDuration);
                        foreach (Image img in _edgeBarsHorizontal) img.rectTransform.localScale = new Vector3(t, 1, 1);
                        foreach (Image img in _edgeBarsVertical) img.rectTransform.localScale = new Vector3(1, t, 1);

                        if (_timer > SearchDuration)
                        {
                            _timer = 0;
                            _state = State.Shoot;
                            Cursor.visible = true;
                            _theRenderer.material.SetVector("_MousePos", new Vector4(99, 99, 0, 1));
                            break;
                        }

                        yield return null;
                    }
                }
                else if (_state == State.Shoot)
                {
                    _shootText.SetActive(true);

                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(false));
                    yield return new WaitForSeconds(1.0f);
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(true));
                    _edgeBarsAll.ForEach(x => x.color = _redColor);
                    _edgeBarsAll.ForEach(x => x.rectTransform.localScale = Vector3.one);
                    _shootText.SetActive(false);

                    HashSet<Transform> markedSpots = new();
                    while (true)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            Vector3 p = _root.Camera.ScreenToWorldPoint(Input.mousePosition);
                            p.z = 0;

                            GameObject shotMark = Instantiate(_shotMarkPrefab, _shotMarksParent);
                            shotMark.transform.position = p.WithZ(-1f);

                            foreach (Transform spotTransform in _spotsParent)
                            {
                                const float SpotRadius = 0.5f;
                                if (Vector3.Distance(p, spotTransform.position) < SpotRadius)
                                {
                                    markedSpots.Add(spotTransform);
                                }
                            }

                            _score = markedSpots.Count;

                        }

                        float t = Mathf.Lerp(1, 0.01f, _timer / ShootDuration);
                        foreach (Image img in _edgeBarsHorizontal) img.rectTransform.localScale = new Vector3(t, 1, 1);
                        foreach (Image img in _edgeBarsVertical) img.rectTransform.localScale = new Vector3(1, t, 1);

                        _timer += Time.deltaTime;
                        if (_timer > ShootDuration)
                        {
                            _timer = 0;
                            _state = State.Score;
                            break;
                        }

                        yield return null;
                    }

                }
                else if (_state == State.Score)
                {
                    _scoreText.SetActive(true);
                    _scoreText.GetComponent<TextMeshProUGUI>().text = $"NICE. YOU HIT {_score}";
                    _theRenderer.enabled = false;
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(false));
                    yield return new WaitForSeconds(2.0f);
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(true));
                    _theRenderer.enabled = true;
                    _scoreText.SetActive(false);
                    _state = State.Countdown;
                }

                yield return null;
            }
        }
    }
}
