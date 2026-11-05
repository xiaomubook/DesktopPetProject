using UnityEngine;

/// <summary>键盘/全局输入。</summary>
public class GameManager : MonoBehaviour
{
    [Header("宠物部件（宠物图片，脚本会自动往上找到宠物本体）")]
    [SerializeField] private Transform fenggePet;
    [SerializeField] private Transform shaolanPet;
    [SerializeField] private Transform sungePet;

    [Header("空格触发的待机动画时长")]
    [SerializeField] private float spaceAnimDuration = 5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayIdleAnim(fenggePet);
            PlayIdleAnim(shaolanPet);
            PlayIdleAnim(sungePet);
        }
    }

    private void PlayIdleAnim(Transform petPart)
    {
        if (petPart == null) return;
        petPart.GetComponentInParent<PetBase>()?.PlayAnim(spaceAnimDuration);
    }
}
