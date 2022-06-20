using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class Projectile : MonoBehaviourPun, IPunObservable, ISavable, IPunInstantiateMagicCallback {
    private Vector3 velocity;
    [SerializeField]
    private GameObject splash;
    [SerializeField]
    private GameObject projectile;
    private ReagentContents contents;
    private HashSet<Collider> ignoreColliders;
    private static Collider[] colliders = new Collider[32];
    private static RaycastHit[] raycastHits = new RaycastHit[32];
    private static HashSet<GenericReagentContainer> hitContainers = new HashSet<GenericReagentContainer>();
    private bool splashed = false;
    void Update() {
        if (splashed) {
            return;
        }

        velocity += Physics.gravity * Time.deltaTime;
        int hits = Physics.RaycastNonAlloc(transform.position, velocity.normalized, raycastHits, velocity.magnitude * Time.deltaTime,
            GameManager.instance.waterSprayHitMask, QueryTriggerInteraction.Ignore);
        int closestHit = -1;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < hits; i++) {
            if (ignoreColliders.Contains(raycastHits[i].collider)) {
                continue;
            }

            if (raycastHits[i].distance < closestDistance) {
                closestHit = i;
                closestDistance = raycastHits[i].distance;
            }
        }

        if (closestHit != -1) {
            transform.position = raycastHits[closestHit].point + raycastHits[closestHit].normal*0.1f;
            OnSplash();
        } else {
            transform.position += velocity * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
        }
    }

    public void LaunchFrom(Rigidbody body) {
        ignoreColliders = new HashSet<Collider>();
        foreach (Collider collider in body.GetComponentsInChildren<Collider>()) {
            ignoreColliders.Add(collider);
        }
    }

    private void OnSplash() {
        splash.SetActive(true);
        projectile.SetActive(false);
        splashed = true;
        hitContainers.Clear();
        int hits = Physics.OverlapSphereNonAlloc(transform.position, 1f, colliders, GameManager.instance.waterSprayHitMask);
        for (int i = 0; i < hits; i++) {
            GenericReagentContainer container = colliders[i].GetComponentInParent<GenericReagentContainer>();
            if (container != null) {
                hitContainers.Add(container);
            }
        }

        float perVolume = contents.volume / hitContainers.Count;
        foreach (GenericReagentContainer container in hitContainers) {
            container.AddMix(contents.Spill(perVolume), GenericReagentContainer.InjectType.Spray);
        }
        if (photonView.IsMine) {
            StartCoroutine(DestroyAfterTime());
        }
        transform.rotation = Quaternion.identity;
    }

    private IEnumerator DestroyAfterTime() {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.Destroy(photonView);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(velocity);
            stream.SendNext(splashed);
        } else {
            velocity = (Vector3)stream.ReceiveNext();
            bool newSplash = (bool)stream.ReceiveNext();
            if (!splashed && newSplash) {
                OnSplash();
            }
            splashed = newSplash;
        }
    }

    public void Save(BinaryWriter writer, string version) {
        writer.Write(velocity.x);
        writer.Write(velocity.y);
        writer.Write(velocity.z);
        writer.Write(splashed);
    }

    public void Load(BinaryReader reader, string version) {
        float vx = reader.ReadSingle();
        float vy = reader.ReadSingle();
        float vz = reader.ReadSingle();
        velocity = new Vector3(vx, vy, vz);
        bool newSplash = reader.ReadBoolean();
        if (!splashed && newSplash) {
            OnSplash();
        }
        splashed = newSplash;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        contents = (ReagentContents)info.photonView.InstantiationData[0];
        velocity = (Vector3)info.photonView.InstantiationData[1];
        splashed = false;
    }
}
