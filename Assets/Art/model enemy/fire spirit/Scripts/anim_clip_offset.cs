using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace namespace_animclip_offset{
public class anim_clip_offset : MonoBehaviour
{
    private Animator animator;
   
    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();
        Play_Animationclip_offset();
    }

    void Play_Animationclip_offset()
    {
        AnimatorClipInfo[] clip_name = animator.GetCurrentAnimatorClipInfo(0);
        if (clip_name == null || clip_name.Length == 0) return;

        AnimationClip anim_clip = clip_name[0].clip;
        if (anim_clip == null) return;

        float time = Random.Range(0f, anim_clip.length);
        int clip_position = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

        animator.Play(clip_position, 0, time / anim_clip.length);
    }
    void Update(){

        
    }
    
}
}
