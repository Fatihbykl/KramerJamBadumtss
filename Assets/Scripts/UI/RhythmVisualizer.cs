using UnityEngine;
using UnityEngine.UI;
using ClockworkGearslinger.Core;

namespace ClockworkGearslinger.UI
{
    /// <summary>
    /// A "Crypt of the NecroDancer" / "Guitar Hero" style scrolling visualizer.
    /// This is vastly more understandable as it lets the player see a continuous stream 
    /// of future beats approaching the center, rather than just breathing in and out.
    /// </summary>
    public class RhythmVisualizer : MonoBehaviour
    {
        [Header("Scrolling Settings")]
        [Tooltip("How many pixels a beat travels. Higher = faster moving notes.")]
        [SerializeField] private float pixelsPerBeat = 400f;
        [Tooltip("How many upcoming beats to show on the screen at once.")]
        [SerializeField] private int beatsAheadToShow = 4;

        [Header("UI References")]
        [Tooltip("Place an empty RectTransform EXACTLY in the center of your screen (on your crosshair). Notes will spawn outside and move towards this point.")]
        [SerializeField] private RectTransform centerContainer;
        
        [Tooltip("A prefab with an Image component to represent a moving beat (e.g. a Chevron > or < )")]
        [SerializeField] private GameObject notePrefab;

        // Object Pools
        private RectTransform[] leftNotes;
        private RectTransform[] rightNotes;
        private Image[] leftImages;
        private Image[] rightImages;

        private void Start()
        {
            if (notePrefab == null)
            {
                Debug.LogError("[RhythmVisualizer] Note Prefab is missing! Please assign a simple UI Image prefab.");
                return;
            }

            // Create object pools for the notes so we don't Instantiate/Destroy every single beat (which ruins performance)
            leftNotes = new RectTransform[beatsAheadToShow + 1];
            rightNotes = new RectTransform[beatsAheadToShow + 1];
            leftImages = new Image[beatsAheadToShow + 1];
            rightImages = new Image[beatsAheadToShow + 1];

            for (int i = 0; i < leftNotes.Length; i++)
            {
                // Instantiate left notes
                GameObject lNote = Instantiate(notePrefab, centerContainer);
                leftNotes[i] = lNote.GetComponent<RectTransform>();
                leftImages[i] = lNote.GetComponent<Image>();
                SetupNote(leftNotes[i]);

                // Instantiate right notes
                GameObject rNote = Instantiate(notePrefab, centerContainer);
                rightNotes[i] = rNote.GetComponent<RectTransform>();
                rightImages[i] = rNote.GetComponent<Image>();
                SetupNote(rightNotes[i]);
            }
        }

        private void SetupNote(RectTransform note)
        {
            // Force anchors and pivot to the center. 
            // This guarantees anchoredPosition(0,0) is EXACTLY the center of the centerContainer.
            note.anchorMin = new Vector2(0.5f, 0.5f);
            note.anchorMax = new Vector2(0.5f, 0.5f);
            note.pivot = new Vector2(0.5f, 0.5f);
        }

        private void Update()
        {
            if (RhythmManager.Instance == null || leftNotes == null) return;

            float currentBeat = RhythmManager.Instance.SongPositionInBeats;
            
            // FloorToInt gives us the integer beat we just passed or are currently inside
            int startBeat = Mathf.FloorToInt(currentBeat);

            for (int i = 0; i < leftNotes.Length; i++)
            {
                // Calculate which specific integer beat this UI element represents
                int targetBeat = startBeat + i;
                
                // How far away is this beat from the exact current song time?
                // Positive = in the future, Negative = in the past
                float beatDifference = targetBeat - currentBeat; 

                // Calculate absolute visual distance from the center (X = 0)
                float distance = beatDifference * pixelsPerBeat;

                // Move the notes
                // Left notes start at negative X (left) and move towards 0
                leftNotes[i].anchoredPosition = new Vector2(-distance, 0f);
                // Right notes start at positive X (right) and move towards 0
                rightNotes[i].anchoredPosition = new Vector2(distance, 0f);

                // --- Handle Colors & Fading ---
                float alpha = 1f;
                Color targetColor = Color.white;

                // 1. Hit Window Feedback: Turn Yellow if they are currently inside the exact hit window!
                if (Mathf.Abs(beatDifference) <= RhythmManager.Instance.InputToleranceInBeats)
                {
                    targetColor = Color.yellow;
                }
                
                // 2. Fade Out: Fade them out quickly after they pass the center crosshair
                if (beatDifference < 0)
                {
                    alpha = 1f - Mathf.Clamp01(-beatDifference * 4f); // Fades out over 0.25 beats
                }
                // 3. Fade In: Fade them in smoothly as they spawn far away at the edge of the screen
                else if (beatDifference > beatsAheadToShow - 1f)
                {
                    alpha = 1f - Mathf.Clamp01(beatDifference - (beatsAheadToShow - 1f));
                }

                // Apply the final color and alpha
                targetColor.a = alpha;
                leftImages[i].color = targetColor;
                rightImages[i].color = targetColor;
            }
        }
    }
}
