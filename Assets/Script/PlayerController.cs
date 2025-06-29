using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float rollDuration = 0.2f;
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public List<GameObject> O;
    public List<GameObject> X;
    public List<GameObject> o;
    public List<GameObject> groundO;
    public List<GameObject> groundX;
    public List<GameObject> groundo;

    public Text moveCountText;
    public Text textScore;
    public Text textHighScore;
    public GameObject canvasWin;

    private string highScoreKey;
    private int moveCount = 0;
    private Button skipButton;
    private bool skipButtonShown = false;

    private bool isMoving = false;
    private bool isWinState = false;
    private string currentBottomFace;
    private string previousBottomFace;
    private float initialY;
    public enum CubeFace { Top, Bottom, Front, Back, Left, Right }

    void Start()
    {
        isWinState = false;

        if (upButton != null)
            upButton.onClick.AddListener(() => OnMoveButtonPressed(Vector3.forward));
        if (downButton != null)
            downButton.onClick.AddListener(() => OnMoveButtonPressed(Vector3.back));
        if (leftButton != null)
            leftButton.onClick.AddListener(() => OnMoveButtonPressed(Vector3.left));
        if (rightButton != null)
            rightButton.onClick.AddListener(() => OnMoveButtonPressed(Vector3.right));

        initialY = transform.position.y;

        UpdateBottomFace();
        previousBottomFace = currentBottomFace;
    }
    void Update()
    {
        if (isMoving || isWinState) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            StartCoroutine(Roll(Vector3.forward));

        if (Input.GetKeyDown(KeyCode.DownArrow))
            StartCoroutine(Roll(Vector3.back));

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            StartCoroutine(Roll(Vector3.left));

        if (Input.GetKeyDown(KeyCode.RightArrow))
            StartCoroutine(Roll(Vector3.right));

        if (currentBottomFace == CubeFace.Top.ToString() || currentBottomFace == CubeFace.Bottom.ToString())
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 0.1f, Vector3.down, 4f);
            bool hasValidSurface = false;
            GameObject groundX = null;

            foreach (var h in hits)
            {
                if (h.collider.CompareTag("Ground") || h.collider.CompareTag("Win") ||
                    h.collider.CompareTag("O") || h.collider.CompareTag("X") || h.collider.CompareTag("o"))
                {
                    hasValidSurface = true;
                    break;
                }
                else if (h.collider.CompareTag("GroundX"))
                {
                    groundX = h.collider.gameObject;
                }
            }
            if (!hasValidSurface && groundX != null)
            {
                float fallDistance = 50f;
                float fallDuration = 1f;
                transform.DOMoveY(transform.position.y - fallDistance, fallDuration).SetEase(Ease.InQuad);
                groundX.transform.DOMoveY(groundX.transform.position.y - fallDistance, fallDuration).SetEase(Ease.InQuad);

                StartCoroutine(ShowLose());
                enabled = false;
            }
        }
    }
    public void OnMoveButtonPressed(Vector3 direction)
    {
        if (!isMoving && !isWinState)
            StartCoroutine(Roll(direction));
    }
    IEnumerator Roll(Vector3 direction)
    {
        isMoving = true;
        
        string preRollBottomFace = currentBottomFace;

        Vector3 pivot = transform.position + direction + Vector3.down * 1f;
        if (currentBottomFace == CubeFace.Top.ToString() || currentBottomFace == CubeFace.Bottom.ToString())
        {
            pivot -= Vector3.up * 1f;
        }
        Vector3 axis = Vector3.Cross(Vector3.up, direction);
        float totalAngle = 0f;

        while (totalAngle < 90f)
        {
            float angle = Mathf.Min(Time.deltaTime * (90f / rollDuration), 90f - totalAngle);
            transform.RotateAround(pivot, axis, angle);
            totalAngle += angle;
            yield return null;
        }
        transform.position = RoundToGrid(transform.position);
        transform.rotation = Quaternion.Euler(RoundToGrid(transform.rotation.eulerAngles));
        
        RaycastHit hit;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 2.1f)
            || (!hit.collider.CompareTag("Ground") && !hit.collider.CompareTag("Win")
            && !hit.collider.CompareTag("O") && !hit.collider.CompareTag("X")
            && !hit.collider.CompareTag("o") && !hit.collider.CompareTag("GroundX")))
        {
            float fallDistance = 50f;
            float fallDuration = 1f;
            transform.DOMoveY(transform.position.y - fallDistance, fallDuration).SetEase(Ease.InQuad);
            isMoving = false;
            StartCoroutine(ShowLose());
        }

        UpdateBottomFace();

        if (preRollBottomFace == CubeFace.Left.ToString() ||
            preRollBottomFace == CubeFace.Right.ToString() ||
            preRollBottomFace == CubeFace.Front.ToString() ||
            preRollBottomFace == CubeFace.Back.ToString())
        {
            if (currentBottomFace == CubeFace.Top.ToString() ||
                currentBottomFace == CubeFace.Bottom.ToString())
            {
                Vector3 newPosition = transform.position;
                if (direction == Vector3.right)
                {
                    newPosition.x += 1f;
                }
                else if (direction == Vector3.left)
                {
                    newPosition.x -= 1f;
                }
                else if (direction == Vector3.forward)
                {
                    newPosition.z += 1f;
                }
                else if (direction == Vector3.back)
                {
                    newPosition.z -= 1f;
                }
                transform.position = RoundToGrid(newPosition);
            }
        }
        moveCount++;
        UpdateMoveCountText();

        if (moveCount >= 50 && !skipButtonShown && skipButton != null)
        {
            skipButtonShown = true;
            skipButton.gameObject.SetActive(true);
            skipButton.transform.DOScale(Vector3.one * 1.2f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        isMoving = false;

        Vector3 topDir = transform.up;
        Vector3 bottomDir = -transform.up;
        Vector3 topPoint = transform.position + topDir * 1.5f;
        Vector3 bottomPoint = transform.position + bottomDir * 1.5f;

        bool winDetected = false;

        if (Physics.Raycast(bottomPoint, bottomDir, out RaycastHit bottomHit, 1.5f) && bottomHit.collider.CompareTag("Win"))
        {
            if (currentBottomFace == CubeFace.Top.ToString() || currentBottomFace == CubeFace.Bottom.ToString())
            {
                isWinState = true;

                yield return new WaitForSeconds(0.2f);
                transform.DOMoveY(transform.position.y - 4f, 1f).SetEase(Ease.InQuad);
                yield return new WaitForSeconds(1f);

                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    ShowWinScore();
                    gm.ShowWinCanvas();
                }

                yield break;
            }
        }
        else if (Physics.Raycast(topPoint, topDir, out RaycastHit topHit, 1.5f) && topHit.collider.CompareTag("Win"))
        {
            winDetected = true;
        }

        if (winDetected)
        {
            isWinState = true;
            yield return new WaitForSeconds(0.2f);
            transform.DOMoveY(transform.position.y - 4f, 1f).SetEase(Ease.InQuad);

            yield return new WaitForSeconds(1f);

            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.ShowWinCanvas();
            }
            yield break;
        }
    }
    Vector3 RoundToGrid(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
    }
    void UpdateBottomFace()
    {
        Vector3[] faceDirections = new Vector3[]
        {
            transform.up,
            -transform.up,
            transform.forward,
            -transform.forward,
            -transform.right,
            transform.right
        };
        CubeFace[] faces = new CubeFace[]
        {
            CubeFace.Top,
            CubeFace.Bottom,
            CubeFace.Front,
            CubeFace.Back,
            CubeFace.Left,
            CubeFace.Right
        };
        float minAngle = float.MaxValue;
        CubeFace bottomFace = CubeFace.Bottom;

        for (int i = 0; i < faceDirections.Length; i++)
        {
            float angle = Vector3.Angle(faceDirections[i], Vector3.down);
            if (angle < minAngle)
            {
                minAngle = angle;
                bottomFace = faces[i];
            }
        }
        currentBottomFace = bottomFace.ToString();

        if (currentBottomFace != previousBottomFace)
        {
            Vector3 newPosition = transform.position;
            if (bottomFace == CubeFace.Left || bottomFace == CubeFace.Right ||
                bottomFace == CubeFace.Front || bottomFace == CubeFace.Back)
            {
                newPosition.y = initialY - 1f;
                transform.position = RoundToGrid(newPosition);
            }
            else if (bottomFace == CubeFace.Top || bottomFace == CubeFace.Bottom)
            {
                newPosition.y = initialY;
                transform.position = RoundToGrid(newPosition);
            }
        }
        previousBottomFace = currentBottomFace;
    }
    public string GetCurrentBottomFace()
    {
        return currentBottomFace;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("O"))
        {
            for (int i = 0; i < O.Count; i++)
            {
                if (O[i] != null && other.gameObject == O[i])
                {
                    if (i < groundO.Count && groundO[i] != null)
                    {
                        bool currentState = groundO[i].activeSelf;
                        groundO[i].SetActive(!currentState);
                    }
                }
            }
        }
        if (other.CompareTag("X"))
        {
            for (int i = 0; i < X.Count; i++)
            {
                if (X[i] != null && other.gameObject == X[i])
                {
                    Vector3 contactDir = (other.transform.position - transform.position).normalized;

                    float dotTop = Vector3.Dot(contactDir, transform.up);
                    float dotBottom = Vector3.Dot(contactDir, -transform.up);

                    if (dotTop > 0.9f || dotBottom > 0.9f)
                    {
                        if (i < groundX.Count && groundX[i] != null)
                        {
                            bool currentState = groundX[i].activeSelf;
                            groundX[i].SetActive(!currentState);
                        }
                    }
                }
            }
        }
        if (other.CompareTag("o"))
        {
            for (int i = 0; i < o.Count; i++)
            {
                if (o[i] != null && other.gameObject == o[i])
                {
                    if (i < groundo.Count && groundo[i] != null)
                    {
                        groundo[i].SetActive(false);
                    }
                }
            }
        }
    }
    IEnumerator ShowLose()
    {
        yield return new WaitForSeconds(0.5f);

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.ShowLoseCanvas();
        }
    }
    public void ResetState()
    {
        isMoving = false;
        isWinState = false;
        moveCount = 0;
        skipButtonShown = false;
        UpdateMoveCountText();

        GameObject skipBtnObj = GameObject.Find("skip");
        if (skipBtnObj != null)
        {
            skipButton = skipBtnObj.GetComponent<Button>();
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() =>
            {
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    gm.LoadLevel(gm.GetCurrentLevelIndex() + 2);
                }
            });
        }
    }
    void UpdateMoveCountText()
    {
        if (moveCountText != null)
            moveCountText.text = "Moves:" + moveCount;
    }
    public void ShowWinScore()
    {
        if (textScore != null)
            textScore.text = "Score: " + moveCount;

        GameManager gm = FindObjectOfType<GameManager>();
        int levelIndex = gm != null ? gm.GetCurrentLevelIndex() : 0;

        highScoreKey = "HighScore_Level_" + levelIndex;
        int best = PlayerPrefs.GetInt(highScoreKey, int.MaxValue);

        if (moveCount < best)
        {
            best = moveCount;
            PlayerPrefs.SetInt(highScoreKey, best);
            PlayerPrefs.Save();
        }

        if (textHighScore != null)
            textHighScore.text = "HighScore: " + (best == int.MaxValue ? "" : best.ToString());
    }
}