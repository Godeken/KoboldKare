using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ObjectiveManager))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PlayAreaEnforcer))]
[RequireComponent(typeof(OcclusionArea))]
[RequireComponent(typeof(MusicManager))]
[RequireComponent(typeof(DayNightCycle))]
[RequireComponent(typeof(CloudPool))]
public class SceneDescriptor : OrbitCameraPivotBase, IPunObservable {
    private static SceneDescriptor instance;
    
    [SerializeField] private Transform[] spawnLocations;
    [SerializeField] private bool canGrabFly = true;
    [SerializeField, SerializeReference, SerializeReferenceButton] private OrbitCameraConfiguration baseCameraConfiguration;
    private AudioListener audioListener;
    private OrbitCamera orbitCamera;
    private bool cheatsEnabled;
    public static void SetCheatsEnabled(bool cheatsEnabled) {
        instance.cheatsEnabled = cheatsEnabled;
    }

    public static bool GetCheatsEnabled() {
        if (instance == null) {
            Debug.LogError("No scene descriptor found, couldn't enable cheats...");
            return false;
        }
        return instance.cheatsEnabled || Application.isEditor;
    }

    private void Awake() {
        //Check if instance already exists
        if (instance == null) {
            //if not, set instance to this
            instance = this;
        } else if (instance != this) {
            //If instance already exists and it's not this:
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
            return;
        }

        //var obj = new GameObject("AutoAudioListener", typeof(AudioListenerAutoPlacement), typeof(AudioListener));
        //audioListener = obj.GetComponent<AudioListener>();
        var orbitCamera = new GameObject("OrbitCamera", typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(OrbitCamera), typeof(AudioListener), typeof(CameraConfigurationListener)) {
            layer = LayerMask.NameToLayer("Default")
        };

        if (baseCameraConfiguration != null) {
            OrbitCamera.AddConfiguration(baseCameraConfiguration);
        }
    }

    public static void GetSpawnLocationAndRotation(out Vector3 position, out Quaternion rotation) {
        if (instance == null || instance.spawnLocations == null || instance.spawnLocations.Length == 0) {
            Debug.Log(instance);
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }
        var t = instance.spawnLocations[Random.Range(0, instance.spawnLocations.Length)];
        Vector3 flattenedForward = t.forward.With(y:0);
        if (flattenedForward.magnitude == 0) {
            flattenedForward = Vector3.forward;
        }
        rotation = Quaternion.FromToRotation(Vector3.forward,flattenedForward.normalized); 
        position = t.position;
    }
    public static bool CanGrabFly() {
        return instance == null || instance.canGrabFly;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(cheatsEnabled);
        } else {
            cheatsEnabled = (bool)stream.ReceiveNext();
        }
    }
}
