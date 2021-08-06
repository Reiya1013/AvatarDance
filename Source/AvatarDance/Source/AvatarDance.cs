using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VRM;

namespace AvatarDance
{
    class AvatarDance
    {
        //AvatarDance
        UnityEngine.Object[] AssetsDance;
        GameObject DanceObject;
        private GameObject DanceVRM;
        private List<string> DanceName = new List<string>();

        // Avatar
        public const int OnlyInThirdPerson = 3;
        public const int LegacyOnlyInFirstPerson = 4;
        public const int OnlyInFirstPerson = 6;
        public const int AlwaysVisible = 10;

        public AvatarDance()
        {
        }

        public void Update()
        {

        }

        public void LastUpdate()
        {
            //if (lipSyncController != null)
            //{
            //    lipSyncController.LateUpdate2();
            //}
        }


        private AssetBundle assetBundle;

        /// <summary>
        /// 変更MaterialChangeファイル一覧取得
        /// </summary>
        /// <returns></returns>
        public string[] GetDanceName()
        {
            DanceName.Clear();
            string[] names = Directory.GetFiles($"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\..\AvatarDance"}", "*.dance");
            string[] rtnNames = new string[names.Length];
            for (int i = 0; i < rtnNames.Length; i++)
            {
                rtnNames[i] = Path.GetFileName(names[i]);
                DanceName.Add(names[i]);
            }

            return rtnNames;
        }

        /// <summary>
        /// AssetBundleからBS_Saderを読み込む
        /// </summary>
        private void DanceLoad()
        {
            //ロード済みならアンロードする
            if (assetBundle) assetBundle.Unload(false);

            //Danceオブジェクトを取得する
            assetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("AvatarDance.avatar_dance.stage"));

            AssetsDance = assetBundle.LoadAllAssets();
            foreach (var asset in AssetsDance)
            {
                if (asset is GameObject gameObject)
                {
                    if (gameObject.name == "Dance_scene")
                        DanceObject = gameObject;
                }
            }
           
        }
        /// <summary>
        /// アセットバンドルを読み込んで保持しておく
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="set"></param>
        private IEnumerator OtherDanceLoad(string filename)
        {
            if (!DanceObject) GameObject.Destroy(DanceObject);
            if (filename == "") yield break;
            //ロード済みならアンロードする
            if (assetBundle) assetBundle.Unload(true);

            Logger.log?.Debug($"OtherDanceLoad {filename}");
            var asyncLoad = AssetBundle.LoadFromFileAsync(filename);
            yield return asyncLoad;
            assetBundle = asyncLoad.assetBundle;
            var assetLoadRequest = assetBundle.LoadAllAssetsAsync();
            yield return assetLoadRequest;
            AssetsDance = assetLoadRequest.allAssets;
            Logger.log?.Debug($"OtherDanceLoad End");

            foreach (var asset in AssetsDance)
            {
                if (asset is GameObject gameObject)
                {
                    if (gameObject.name == "Dance_scene")
                    {
                        DanceObject = gameObject;
                    }
                }
            }
        }


        /// <summary>
        /// VRM取得して、Danceモーションのセット
        /// </summary>
        public IEnumerator GetVRMAndSetDance(int selectNo)
        {
            //選択したDanceをロードする
            yield return OtherDanceLoad(DanceName[selectNo]);

            //読み込み失敗で終了
            if (DanceObject == null) yield break;

            //VRMなくても終了
            GameObject Avatar = GameObject.Find("VRM");
            if (Avatar == null) yield break;
            VRMCopyDestroy();

            //SpringBoneを一旦OFF
            GameObject secondary = Avatar.transform.Find("secondary").gameObject;
            secondary.SetActive(false);

            if (!SetupStage)
            {
                var game = GameObject.Find("AvatarDance");
                SetupStage = GameObject.Instantiate(new GameObject("OnStage"), new Vector3(0f, 0f, 0f), Quaternion.identity, game.GetComponent<Transform>()) as GameObject;
                
            }
            SetupStage.SetActive(false);
            
            //GameObject activeFalse1 = GameObject.Find("VMCAvatar/RoomAdjust");

            DanceVRM = GameObject.Instantiate(Avatar, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;
            DanceVRM.name = "DanceVRM";

            //レイヤーを全体から見えるものに変更
            foreach (var skinnedMeshRenderer in DanceVRM.GetComponentsInChildren<Renderer>(true))
            {
                if (skinnedMeshRenderer.gameObject.layer != AlwaysVisible)
                {
                    skinnedMeshRenderer.gameObject.layer = AlwaysVisible;
                }
            }

            //コピーされないSpringBoneのパラメータをコピー
            SpringBoneCopy(Avatar, DanceVRM);

            //SpringBoneの実行
            GameObject copyAvatarsecondary = DanceVRM.transform.Find("secondary").gameObject;
            copyAvatarsecondary.SetActive(true);

            OnAvatarDance();

            secondary.SetActive(true);
            SetupStage.SetActive(true);

            SetupEnd = true;

        }

        public void Play()
        {
            //animator.SetBool("Open", !animator.GetBool("Open"));
            SetupStage.SetActive(true);
        }

        /// <summary>
        /// SpringBoneのパラメータをコピーする
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="to"></param>
        private void SpringBoneCopy(GameObject owner, GameObject to)
        {
            Transform[] transformList = to.GetComponentsInChildren<Transform>();

            var colliders = owner.GetComponentsInChildren<VRMSpringBoneColliderGroup>();
            foreach (var c in colliders)
            {
                var targetTransform = transformList.FirstOrDefault(x => x.name == c.transform.name);
                if (targetTransform != null)
                {
                    var col = targetTransform.GetComponentsInChildren<VRMSpringBoneColliderGroup>();
                    for (int i = 0; i < col[0].Colliders.Length; i++)
                    {
                        col[0].Colliders[i] = new VRMSpringBoneColliderGroup.SphereCollider();
                        col[0].Colliders[i].Offset = c.Colliders[i].Offset;
                        col[0].Colliders[i].Radius = (float)(c.Colliders[i].Radius * 1);
                    }
                }
                else
                {
                    Debug.LogWarning("Not found VRMSpringBoneColliderGroup bone->" + c.gameObject.name);
                }
            }

            foreach (var c in transformList)
            {
                c.rotation = new Quaternion();
            }

        }

        GameObject SetupStage;
        GameObject Stage;
        GameObject Bgm;
        GameObject Notes;
        GameObject DustPS;
        GameObject PileOfNotes;
        GameObject MenuBgm;
        GameObject Timeline;
        GameObject Apends;
        public  AudioClip BgmClip;
        private Animator VrmAnimator;

        public bool SetupEnd { get; set; }

        private void OnAvatarDance()
        {


            //Stageをセット
            var stage = DanceObject.transform.Find("Stage").gameObject;
            Stage = GameObject.Instantiate(stage, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;
            Logger.log?.Debug($"OnAvatarDance Stage");

            //BGMをセット
            var bgm = DanceObject.transform.Find("BGM").gameObject;
            Bgm = GameObject.Instantiate(bgm, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;
            BgmClip = Bgm.GetComponent<AudioSource>().clip;
            Logger.log?.Debug($"OnAvatarDance Bgm");

            //特殊処理用があれば追加
            var append = DanceObject.transform.Find("Animation/Apends");
            if (append)
            {
                Apends = GameObject.Instantiate(append.gameObject, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;
                Logger.log?.Debug($"OnAvatarDance Apends");

                //Handに関してはオブジェクトの設定を行う
                var hand = Apends.transform.Find("Hand");
                if (hand)
                {
                    Animator ani = DanceVRM.GetComponent<Animator>();
                    Transform lhand = ani.GetBoneTransform(HumanBodyBones.LeftHand);
                    Transform rhand = ani.GetBoneTransform(HumanBodyBones.RightHand);
                    var lh = GameObject.Instantiate(hand.gameObject, Vector3.zero, Quaternion.identity, lhand);
                    var rh = GameObject.Instantiate(hand.gameObject, Vector3.zero, Quaternion.identity, rhand);
                    //ずれるので戻す
                    lh.transform.localPosition = Vector3.zero;
                    rh.transform.localPosition = Vector3.zero;

                    //Handオブジェクト自体はコピー用のオブジェクト格納なので非アクティブにしておく
                    hand.gameObject.SetActive(false);
                }
            }

            //スクリプトがある場合追加(自分が追加した場合しか対応しない)
            ScriptAttach();



            //Animationがなければ作成
            if (!DanceVRM.GetComponent<Animator>())
                DanceVRM.AddComponent<Animator>();
            Logger.log?.Debug($"OnAvatarDance CopyVRM");

            Animator vrmAnimator = DanceVRM.GetComponent<Animator>();
            vrmAnimator.applyRootMotion = true;
            Logger.log?.Debug($"OnAvatarDance CopyVRM vrmAnimator");

            //GameObject grandChild = avatarDanve["mirai_scene"].transform.Find("mirai_scene").GetComponent<Animator>();
            var animation = DanceObject.transform.Find("Animation/Animator");
            Logger.log?.Debug($"OnAvatarDance animation");

            //AnimatorにControllerが設定されていか取得して確認する
            var animator = animation.GetComponent<Animator>();
            if (animator)
                if (animator.runtimeAnimatorController)
                {
                    //AnimationControllerをセットしてAnimationで移動を許可する
                    vrmAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
                }

            Logger.log?.Debug($"OnAvatarDance animation set end");

            var timeline = DanceObject.transform.Find("Animation/TimeLine");
            if (timeline)
            {
                //TimelineのPlayableDirector内に"Dance Animation Track"があると課程して、そこにVRMのAnimatorをBindする
                PlayableDirector director = timeline.GetComponent<PlayableDirector>();
                //TimelineAsset timelineAsset = timeline.GetComponent<TimelineAsset>();
                if (director) 
                    if (director.playableAsset)
                        foreach (PlayableBinding bind in director.playableAsset.outputs)
                        {
                            if (bind.streamName == "Dance Animation Track")
                            {
                                director.SetGenericBinding(bind.sourceObject, vrmAnimator);
                            }
                            else if (bind.streamName == "BGM Audio Track")
                            {
                                director.SetGenericBinding(bind.sourceObject, BgmClip);
                            }
                        }

                //Timelineをセット
                Timeline = GameObject.Instantiate(timeline.gameObject, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;
                director.Play();
            }
            Logger.log?.Debug($"OnAvatarDance TimeLine set end");



            //VRMを指定座標に移動


            ////Stageをセット
            //var stage = DanceObject.transform.Find("Stage").gameObject;
            //Stage = GameObject.Instantiate(stage, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;

            ////BGMをセット
            //var bgm = DanceObject.transform.Find("BGM").gameObject;
            //Bgm = GameObject.Instantiate(bgm, new Vector3(0f, 0f, 0f), Quaternion.identity, SetupStage.GetComponent<Transform>()) as GameObject;
            //BgmClip = Bgm.GetComponent<AudioSource>().clip;
            /* 
            消せばいい奴ら
            "MenuEnvironmentCore/PlayersPlace",
            "MenuEnvironmentCore/GroundCollider",
            "MenuEnvironmentManager/DefaultMenuEnvironment/BasicMenuGround",
            "MenuEnvironmentManager/MultiplayerLobbyEnvironment/LobbyAvatarPlace/FlyingPlatform",
             
             */

            //床とかいらないの消す
            if (!Notes) Notes = GameObject.Find("MenuEnvironmentManager/DefaultMenuEnvironment/Notes");
            if (!PileOfNotes) PileOfNotes = GameObject.Find("MenuEnvironmentManager/DefaultMenuEnvironment/PileOfNotes");
            if (!DustPS) DustPS = GameObject.Find("MenuMainCamera/DustPS");
            if (!MenuBgm) MenuBgm = GameObject.Find("SongPreviewPlayer");
            Notes.SetActive(false);
            PileOfNotes.SetActive(false);
            DustPS.SetActive(false);

            foreach (AudioSource ob in MenuBgm.GetComponentsInChildren<AudioSource>(true))
            {
                ob.Stop();
            }

            MenuBgm.SetActive(false);

        }

        //専用スクリプト郡
        LipSyncController lipSyncController;
        CameraSwitcher cameraSwitcher;
        ForceAspectRatio forceAspectRatio;
        MirrorReflection mirrorReflection;

        private void ScriptAttach()
        {

            //すべてのオブジェクトの名称に[_Sc_]が含まれてるか確認し、その後ろに付いてる名称が追加するスクリプトの名称としてアタッチする
            foreach (GameObject childTransform in SetupStage.GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToArray())
            {
                if (childTransform.name.Contains("_Sc_"))
                {
                    var names = childTransform.name.Substring(childTransform.name.IndexOf("_Sc_") + 4);
                    if (names == "LipSyncController")
                    {
                        Logger.log?.Debug($"LipSyncController Attach");
                        childTransform.AddComponent<LipSyncController>();
                        var setting = lipSyncController = childTransform.transform.GetComponent<LipSyncController>();
                        //var setting = new LipSyncController();
                        ////var setting = lipSyncController;
                        setting.targetName = "DanceVRM";
                        setting.BlendShapeA = BlendShapePreset.A;
                        setting.BlendShapeI = BlendShapePreset.I;
                        setting.BlendShapeU = BlendShapePreset.U;
                        setting.BlendShapeE = BlendShapePreset.E;
                        setting.BlendShapeO = BlendShapePreset.O;

                        //Node用オブジェクト取得
                        setting.nodeA = childTransform.transform.Find("rsyncRoot/rsync_A");
                        setting.nodeI = childTransform.transform.Find("rsyncRoot/rsync_I");
                        setting.nodeU = childTransform.transform.Find("rsyncRoot/rsync_U");
                        setting.nodeE = childTransform.transform.Find("rsyncRoot/rsync_E");
                        setting.nodeO = childTransform.transform.Find("rsyncRoot/rsync_O");


                        setting.weightCurve = AnimationCurve.Linear(
                                                                    timeStart: 0.01f,
                                                                    valueStart: 0f,
                                                                    timeEnd: 0.03f,
                                                                    valueEnd: 1f
                                                                    );


                        //初期処理する
                        setting.StartUp(DanceVRM);
                    }
                    else if (names == "CameraSwitcher")
                    {
                        Logger.log?.Debug($"CameraSwitcher Attach");
                        childTransform.AddComponent<CameraSwitcher>();
                        var setting = cameraSwitcher = childTransform.transform.GetComponent<CameraSwitcher>();
                        setting.targetName = "Head";
                        //初期処理する
                        setting.StartUp(DanceVRM);
                    }
                    else if (names == "ForceAspectRatio")
                    {
                        Logger.log?.Debug($"ForceAspectRatio Attach");
                        childTransform.AddComponent<ForceAspectRatio>();
                        var setting = forceAspectRatio = childTransform.transform.GetComponent<ForceAspectRatio>();
                        setting.horizontal = 4;
                        setting.vertical = 3;
                    }
                    else if (names == "MirrorReflection")
                    {
                        Logger.log?.Debug($"MirrorReflection Attach");
                        childTransform.AddComponent<MirrorReflection>();
                        var setting = mirrorReflection = childTransform.transform.GetComponent<MirrorReflection>();
                        setting.m_TextureSize = 1024;
                        setting.m_DisablePixelLights = true;
                        setting.m_ClipPlaneOffset = 0;

                        //2個めに必要なMaterialを持ってきてる
                        var mesh = childTransform.transform.GetComponent<MeshRenderer>();
                        setting.m_matCopyDepth = mesh.materials[1];

                        //RenderTexture再設定
                        RenderTexture renderTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
                        setting.m_ReflectionTexture = renderTexture;
                        mesh.materials[0].mainTexture = renderTexture;
                        RenderTexture renderTextureD = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth);
                        setting.m_ReflectionDepthTexture = renderTextureD;
                    }
                    else if (names == "BackScreenRenderSet")
                    {
                        Logger.log?.Debug($"BackScreenRenderSet Attach");
                        childTransform.AddComponent<BackScreenRenderSet>();
                    }
                    else if (names.Contains("ConstantMotion"))
                    {
                        var param = names.Substring(names.IndexOf("[")  + 1, names.IndexOf("]") - names.IndexOf("[") - 1);
                        Logger.log?.Debug($"ConstantMotion Attach {param}");
                        childTransform.AddComponent<ConstantMotion>();
                        var setting = childTransform.transform.GetComponent<ConstantMotion>();
                        setting.rotation.velocity = Int32.Parse(param);
                        setting.rotation.randomness = 0.3f;
                        setting.rotation.mode = ConstantMotion.TransformMode.YAxis;
                        setting.position.mode = ConstantMotion.TransformMode.Off;
                        setting.useLocalCoordinate = true;
                    }
                    else if (names.Contains("SelfDestruction"))
                    {
                        var param = names.Substring(names.IndexOf("[") + 1, names.IndexOf("]") - names.IndexOf("[") - 1);
                        Logger.log?.Debug($"SelfDestruction Attach {param}");
                        childTransform.AddComponent<SelfDestruction>();
                        var setting = childTransform.transform.GetComponent<SelfDestruction>();
                        setting.conditionType = SelfDestruction.ConditionType.Time;
                        setting.lifetime = float.Parse(param);
                        setting.enabled = false;
                    }
                    else if (names.Contains("Spawner_One"))
                    {
                        Logger.log?.Debug($"Spawner_One Attach");
                        childTransform.AddComponent<Spawner_One>();
                        var setting = childTransform.transform.GetComponent<Spawner_One>();

                        var tagetGameObject = Apends.transform.Find("Effect/Laser_Sc_SelfDestruction[1.5]").gameObject;
                        Logger.log?.Debug($"Spawner_One Effect Setup {tagetGameObject is null}");
                        setting.StartUp(tagetGameObject);
                    }
                    else if (names.Contains("Spawner_Multi"))
                    {
                        Logger.log?.Debug($"Spawner_Multi Attach");
                        childTransform.AddComponent<Spawner_Multi>();
                        var setting = childTransform.transform.GetComponent<Spawner_Multi>();

                        var tagetGameObject = Apends.transform.Find("Effect/Laser_Sc_SelfDestruction[1.5]").gameObject;
                        Transform[] point = new Transform[14];
                        int setpoint = 0;
                        foreach (Transform childTransformPoint in childTransform.GetComponentsInChildren<Transform>())
                        {
                            if (childTransformPoint.name == "Point")
                            {
                                point[setpoint] = childTransformPoint;
                                setpoint += 1;
                            }
                        }
                        setting.StartUp(tagetGameObject, point, Apends);
                    }

                    else if (names.Contains("VariableMotion"))
                    {
                        Logger.log?.Debug($"VariableMotion Attach");
                        childTransform.AddComponent<VariableMotion>();
                        var setting = childTransform.transform.GetComponent<VariableMotion>();
                        setting.rotation.speed = 0.1f;
                        setting.rotation.randomness = 0;
                        setting.rotation.amplitude = 70;
                        setting.rotation.curve = AnimationCurve.EaseInOut(
                                                                    timeStart: 0f,
                                                                    valueStart: 0f,
                                                                    timeEnd: 1f,
                                                                    valueEnd: 1f
                                                                    );
                        setting.rotation.curve.preWrapMode = WrapMode.PingPong;

                        setting.rotation.mode = VariableMotion.TransformMode.YAxis;
                        setting.position.mode = VariableMotion.TransformMode.Off;
                        setting.scale.mode = VariableMotion.TransformMode.Off;

                        setting.useLocalCoordinate = true;
                    }
                    else if (names == "JitterMotion")
                    {
                        Logger.log?.Debug($"JitterMotion Attach");
                        childTransform.AddComponent<JitterMotion>();
                    }

                    

                }
            }
        }


        /// <summary>
        /// コピーしたVRMをデストロイする
        /// </summary>
        public void VRMCopyDestroy()
        {

            if (DanceVRM != null) GameObject.Destroy(DanceVRM);
            if (Notes != null) Notes.SetActive(true);
            if (PileOfNotes != null) PileOfNotes.SetActive(true);
            if (DustPS != null) DustPS.SetActive(true);
            if (Stage != null) GameObject.Destroy(Stage);
            if (Bgm != null) GameObject.Destroy(Bgm);
            if (Timeline != null) GameObject.Destroy(Timeline);
            if (SetupStage != null) GameObject.Destroy(SetupStage);
            if (Apends != null) GameObject.Destroy(Apends);

            //追加オブジェクト分生きてたら削除する


            if (MenuBgm != null)
                foreach (AudioSource ob in MenuBgm.GetComponentsInChildren<AudioSource>(true))
                {
                    ob.Play();
                }
        }
    }
}
