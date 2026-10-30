using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace SafeTouch.Services
{
    /// <summary>
    /// Optional anonymous progress sync to the SafeTouch API (backend/ in this
    /// repo, Node.js + Express + MongoDB Atlas). Off unless enabled in the
    /// build AND a parent switches it on in the parent section. Sends only a
    /// random device id, the age band and lesson results.
    /// </summary>
    public class CloudSyncService : MonoBehaviour
    {
        [SerializeField] string baseUrl = "http://localhost:3000";
        [SerializeField] bool enabledInBuild = false;
        [SerializeField] int timeoutSeconds = 10;

        public string LastStatus { get; private set; }

        [Serializable]
        public class SyncPayload
        {
            public string deviceId;
            public string ageBand;
            public string language;
            public List<int> completedLevels;
            public List<int> quizScores;
            public List<string> badges;
            public List<string> trustedAdultRoles;
            public List<ScenarioResponse> scenarioResponses;

            public static SyncPayload From(Progress p)
            {
                return new SyncPayload
                {
                    deviceId = p.userId,
                    ageBand = p.ageGroup,
                    language = p.language,
                    completedLevels = p.completedLevels,
                    quizScores = p.quizScores,
                    badges = p.badgesEarned,
                    trustedAdultRoles = p.trustedAdults,
                    scenarioResponses = p.scenarioResponses
                };
            }
        }

        public IEnumerator Upload(Progress progress)
        {
            if (!enabledInBuild || progress == null || !progress.cloudSyncConsent)
            {
                LastStatus = "skipped";
                yield break;
            }

            var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(SyncPayload.From(progress)));
            using (var req = new UnityWebRequest(ApiAddress.Resolve(baseUrl).TrimEnd('/') + "/api/progress", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = ApiAddress.Timeout(timeoutSeconds);
                yield return req.SendWebRequest();

                LastStatus = req.result == UnityWebRequest.Result.Success ? "synced" : "failed: " + req.error;
                Debug.Log("[SafeTouch] Cloud sync " + LastStatus);
            }
        }

        /// <summary>Deletes this device's record when a parent switches sync off.</summary>
        public IEnumerator Withdraw(string deviceId)
        {
            if (!enabledInBuild || string.IsNullOrEmpty(deviceId)) yield break;
            using (var req = UnityWebRequest.Delete(ApiAddress.Resolve(baseUrl).TrimEnd('/') + "/api/progress/" + deviceId))
            {
                req.timeout = ApiAddress.Timeout(timeoutSeconds);
                yield return req.SendWebRequest();
                LastStatus = req.result == UnityWebRequest.Result.Success ? "withdrawn" : "failed: " + req.error;
            }
        }
    }
}
