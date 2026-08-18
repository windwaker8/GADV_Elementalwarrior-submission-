using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//done

public class RollingDigit : MonoBehaviour
{
    public Sprite[] digitFrames;     // Array of 132 sub-sprites
    public Image digitImage;
    public int framesPerDigit = 12;  // 12 sub-frames per digit step
    public int blankFrameIndex = 120; // Sprite index where blank frames start

    public int currentDigit = 0; 
    private float tickDuration = 0.16f;
    private bool isRolling = false;

    void Awake()
    {
        EnsureImageReference();
    }

    private void EnsureImageReference()
    {
        if (digitImage == null) digitImage = GetComponent<Image>();
        if (digitImage == null) digitImage = GetComponentInChildren<Image>();
    }

    public void SetTickDuration(float duration) => tickDuration = duration;
    public bool IsRolling() => isRolling;

    public void SnapToCurrent()
    {
        StopAllCoroutines();
        isRolling = false;
        UpdateSprite();
    }

   public void RollToDigit(int target, bool isTickingUp)
{
    if (isRolling) return;

    int start = currentDigit;
    currentDigit = target;

    StartCoroutine(AnimateSubFrames(start, target, isTickingUp));
}

private IEnumerator AnimateSubFrames(int startDigit, int targetDigit, bool isTickingUp)
{
    isRolling = true;

    // Handle rolling down to blank (-1)
    if (targetDigit == -1)
    {
        // Force backward step from current digit (e.g., 1) down to 0, then to blank
        int startFrame = startDigit * framesPerDigit; 
        int totalFramesToMove = framesPerDigit; 
        float delay = tickDuration / framesPerDigit;

        for (int i = 0; i < totalFramesToMove; i++)
        {
            startFrame--;
            
            // If frame drops below 0, wrap directly to blankFrameIndex (e.g., 120)
            if (startFrame < 0) 
                startFrame = blankFrameIndex;

            if (startFrame >= 0 && startFrame < digitFrames.Length)
            {
                digitImage.sprite = digitFrames[startFrame];
            }

            yield return new WaitForSeconds(delay);
        }
    }
    else
    {
        // Standard 0-9 subframe animation
        int maxFrames = 10 * framesPerDigit;
        int currentFrame = startDigit < 0 ? blankFrameIndex : startDigit * framesPerDigit;
        int stepDirection = isTickingUp ? 1 : -1;
        float delay = tickDuration / framesPerDigit;

        for (int i = 0; i < framesPerDigit; i++)
        {
            if (currentFrame == blankFrameIndex && isTickingUp)
            {
                currentFrame = 0;
            }
            else
            {
                currentFrame = (currentFrame + stepDirection + maxFrames) % maxFrames;
            }

            if (currentFrame >= 0 && currentFrame < digitFrames.Length)
            {
                digitImage.sprite = digitFrames[currentFrame];
            }

            yield return new WaitForSeconds(delay);
        }
    }

    UpdateSprite();
    isRolling = false;
}

    private void UpdateSprite()
    {
        EnsureImageReference();

        if (digitImage == null || digitFrames == null || digitFrames.Length == 0) 
            return;

        if (currentDigit < 0 || currentDigit > 9)
        {
            if (blankFrameIndex < digitFrames.Length)
            {
                digitImage.sprite = digitFrames[blankFrameIndex];
            }
        }
        else
        {
            int frameIndex = currentDigit * framesPerDigit;
            if (frameIndex < digitFrames.Length)
            {
                digitImage.sprite = digitFrames[frameIndex];
            }
        }
    }
}