using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.VFX;

public class ElectricBlender : SuckingMachine {
    [SerializeField]
    private FluidStream stream;
    [SerializeField]
    private GenericReagentContainer container;
    
    [SerializeField] private VisualEffect poof;
    [SerializeField] private AudioPack grindSound;
    private AudioSource source;

    protected override void Awake() {
        base.Awake();
        container.OnChange.AddListener(OnFluidChanged);
        if (source == null) {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.maxDistance = 10f;
            source.minDistance = 0.2f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.spatialBlend = 1f;
            source.loop = false;
            source.enabled = false;
        }
    }

    private void OnDestroy() {
        if (container != null) {
            container.OnChange.RemoveListener(OnFluidChanged);
        }
    }

    void OnFluidChanged(ReagentContents contents, GenericReagentContainer.InjectType injectType) {
        stream.OnFire(container);
    }

    protected override void OnTriggerEnter(Collider other) {
        if (!constructed) {
            return;
        }

        base.OnTriggerEnter(other);
    }

    [PunRPC]
    protected override IEnumerator OnSwallowed(int viewID) {
        if (!constructed) {
            yield break;
        }
        PhotonView view = PhotonNetwork.GetPhotonView(viewID);
        poof.SendEvent("TriggerPoof");
        source.enabled = true;
        grindSound.Play(source);
        StartCoroutine(WaitThenDisableSound());
        GenericReagentContainer otherContainer = view.GetComponent<GenericReagentContainer>();
        if (otherContainer != null) {
            container.AddMixRPC(otherContainer.GetContents(), viewID);
        }
        yield return base.OnSwallowed(viewID);
    }

    IEnumerator WaitThenDisableSound() {
        while(source.isPlaying) {
            yield return null;
        }
        source.enabled = false;
    }
}
