// TODO
// - sfx / music
// - maybe: prevent focusing on one spot?
// - make a

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
        [SerializeField] private JamKit _jamkit;
        [SerializeField] private MeshRenderer _theRenderer;
        [SerializeField] private BoxCollider2D _spotVolume;
        [SerializeField] private GameObject _spotPrefab;
        [SerializeField] private GameObject _shotMarkPrefab;
        [SerializeField] private Transform _spotsParent;
        [SerializeField] private Transform _shotMarksParent;
        [SerializeField] private GameObject _shotLinePrefab;
        [SerializeField] private Transform _shotLinesParent;
        [SerializeField] private GameObject _searchText;
        [SerializeField] private GameObject _shootText;
        [SerializeField] private GameObject _scoreText;
        [SerializeField] private Color _redColor;
        [SerializeField] private Color _greenColor;
        [SerializeField] private Color _whiteColor;
        [SerializeField] private Transform _inventoryParent;
        [SerializeField] private GameObject _inventoryItemPrefab;
        [SerializeField] private List<Image> _edgeBarsAll;
        [SerializeField] private Image[] _edgeBarsHorizontal;
        [SerializeField] private Image[] _edgeBarsVertical;

        State _state;
        float _timer = 0;

        float _score = 0;
        int _shotsRemaining;

        const int SpotCount = 5;
        const float SearchDuration = 10.0f;
        const float ShootDuration = 7.0f;
        const float SpotScale = 1.0f;
        const float HoleRadius = 0.5f;

        public void Setup()
        {
        }

        public void ResetGame()
        {
            Cursor.visible = false;
            _state = State.Countdown;
            _theRenderer.material.SetVector("_MousePos", new Vector4(99, 99, 0, 1));
            _theRenderer.material.SetFloat("_HoleRadius", HoleRadius);

            StartCoroutine(Tick());
        }

        void PlayTextAnim(GameObject textGo, float stayDuration)
        {
            const float TextOffset = 10;
            textGo.transform.position = textGo.transform.position.WithX(-TextOffset);
            _jamkit.TweenSeq(new TweenBase[]
            {
                new TweenMove(textGo.transform, Vector3.zero, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1)),
                new TweenDelay(stayDuration),
                new TweenMove(textGo.transform, new (TextOffset, 0, 0), 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1)),
            });
        }

        IEnumerator Tick()
        {
            while (true)
            {
                if (_state == State.Countdown)
                {
                    foreach (Transform t in _spotsParent) Destroy(t.gameObject);
                    foreach (Transform t in _shotMarksParent) Destroy(t.gameObject);
                    foreach (Transform t in _shotLinesParent) Destroy(t.gameObject);
                    _edgeBarsAll.ForEach(x => x.rectTransform.localScale = Vector3.one);
                    _score = 0;

                    List<Vector2> spotPositions = new();
                    Vector2 s = _spotVolume.size / 2;
                    for (int i = 0; i < SpotCount; i++)
                    {
                        Vector2 randomPos;
                        do randomPos = new(Random.Range(-s.x, s.x), Random.Range(-s.y, s.y));
                        while (spotPositions.Exists(spotPos => Vector2.Distance(spotPos, randomPos) < 2 * SpotScale));

                        spotPositions.Add(randomPos);
                    }
                    foreach (Vector2 pos in spotPositions)
                    {
                        GameObject spotGo = Instantiate(_spotPrefab, _spotsParent);
                        spotGo.transform.position = pos;
                        spotGo.transform.localScale *= SpotScale;
                    }

                    PlayTextAnim(_searchText, 0.5f);
                    _searchText.SetActive(true);
                    yield return new WaitForSeconds(1.0f);
                    _searchText.SetActive(false);

                    _state = State.Search;
                }
                else if (_state == State.Search)
                {
                    _jamkit.PlaySfx("PingLow");
                    Cursor.visible = false;
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(true));
                    _edgeBarsAll.ForEach(x => x.color = _whiteColor);
                    _edgeBarsAll.ForEach(x => x.rectTransform.localScale = Vector3.one);

                    while (true)
                    {
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
                    _jamkit.PlaySfx("PingHigh");
                    void SetInventory(int itemCount)
                    {
                        foreach (Transform item in _inventoryParent) Destroy(item.gameObject);
                        for (int i = 0; i < itemCount; i++)
                        {
                            Instantiate(_inventoryItemPrefab, _inventoryParent);
                        }
                    }
                    _shootText.SetActive(true);
                    _shotsRemaining = SpotCount;

                    _inventoryParent.gameObject.SetActive(true);
                    SetInventory(_shotsRemaining);

                    PlayTextAnim(_shootText, 0.5f);
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(false));
                    yield return new WaitForSeconds(1.0f);
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(true));
                    _edgeBarsAll.ForEach(x => x.color = _redColor);
                    _edgeBarsAll.ForEach(x => x.rectTransform.localScale = Vector3.one);
                    _shootText.SetActive(false);

                    List<Vector2> shots = new();
                    while (true)
                    {
                        if (Input.GetMouseButtonDown(0) && _shotsRemaining > 0) // Shoot
                        {
                            _jamkit.PlaySfx("Shoot");
                            Vector3 p = _root.Camera.ScreenToWorldPoint(Input.mousePosition);
                            p.z = 0;

                            GameObject shotMarkGo = Instantiate(_shotMarkPrefab, _shotMarksParent);
                            shotMarkGo.transform.position = p.WithZ(-1f);
                            shots.Add(p);
                            _shotsRemaining--;
                            SetInventory(_shotsRemaining);
                        }

                        float t = Mathf.Lerp(1, 0.01f, _timer / ShootDuration);
                        foreach (Image img in _edgeBarsHorizontal) img.rectTransform.localScale = new Vector3(t, 1, 1);
                        foreach (Image img in _edgeBarsVertical) img.rectTransform.localScale = new Vector3(1, t, 1);

                        _timer += Time.deltaTime;
                        if (_timer > ShootDuration)
                        {
                            _score = CalculateScore(_spotsParent, shots, out var highestScores);
                            foreach ((Vector2 shot, Transform spot) in highestScores)
                            {
                                LineRenderer line = Instantiate(_shotLinePrefab, _shotLinesParent).GetComponent<LineRenderer>();
                                line.SetPositions(new Vector3[] { shot, spot.position });
                            }

                            _timer = 0;
                            _state = State.Score;
                            _inventoryParent.gameObject.SetActive(false);
                            break;
                        }

                        yield return null;
                    }

                }
                else if (_state == State.Score)
                {
                    _jamkit.PlaySfx("Score");
                    _scoreText.SetActive(true);
                    string scoreString = $"SCORE: {_score:F1}";
                    _scoreText.GetComponent<TextMeshProUGUI>().text = scoreString;
                    _theRenderer.enabled = false;
                    _edgeBarsAll.ForEach(x => x.gameObject.SetActive(false));
                    while (true)
                    {
                        if (Input.anyKeyDown) break;
                        yield return null;
                    }
                    _theRenderer.enabled = true;
                    _scoreText.SetActive(false);
                    _state = State.Countdown;
                }

                yield return null;
            }
        }

        private float CalculateScore(Transform spotsParent, List<Vector2> shots, out List<(Vector2, Transform)> highestScores)
        {
            List<(Vector2, Transform, float)> shotSpotPairs = new();

            foreach (Vector2 shot in shots)
            {
                foreach (Transform spot in spotsParent)
                {
                    shotSpotPairs.Add((shot, spot, Vector3.Distance(shot, spot.position)));
                }
            }

            shotSpotPairs.Sort((x, y) => x.Item3.CompareTo(y.Item3));

            highestScores = new();
            float totalScore = 0;

            foreach ((Vector2 shot, Transform spot, float dist) in shotSpotPairs)
            {
                if (highestScores.Exists(pair => pair.Item1 == shot || pair.Item2 == spot))
                {
                    continue;
                }

                highestScores.Add((shot, spot));

                float score = (1.0f / dist);
                score = Mathf.Max(score, 0.1f);

                const float FeelGoodCoeff = 10.0f;
                score *= FeelGoodCoeff;

                totalScore += score;
            }

            return totalScore;
        }
    }
}
