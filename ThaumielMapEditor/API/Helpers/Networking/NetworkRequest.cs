// -----------------------------------------------------------------------
// <copyright file="NetworkRequest.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using MEC;
using ThaumielMapEditor.API.Enums;
using UnityEngine.Networking;

namespace ThaumielMapEditor.API.Helpers.Networking
{
    public abstract class NetworkRequest
    {

        public static IEnumerable<NetworkRequest> ActiveRequests = [];
        public static IReadOnlyList<NetworkRequest> RunningRequests = ActiveRequests.Where(r => r.Handle.IsRunning).ToList().AsReadOnly();

        public static void Cleanup()
        {
            foreach (NetworkRequest request in ActiveRequests.Where(r => !r.Handle.IsRunning).ToArray())
            {
                request.RemoveRequest();
            }
        }

        public void RemoveRequest()
        {
            List<NetworkRequest> requests = ActiveRequests.ToList();
            requests.Remove(this);
            ActiveRequests = requests;
        }

        public CoroutineHandle Handle;

        /// <summary>
        /// Gets the name of the request.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the TMEAPI endpoint to send the request to.
        /// </summary>
        public virtual string Endpoint => string.Empty;

        /// <summary>
        /// Gets the custom url to send the request to.
        /// </summary>
        public virtual string CustomUrl => string.Empty;

        /// <summary>
        /// Gets the type of request to use.
        /// </summary>
        public abstract RequestType Method { get; }

        /// <summary>
        /// Gets or sets the payload to be sent with the request.
        /// </summary>
        public virtual Dictionary<string, object> Payload { get; set; } = [];

        /// <summary>
        /// Gets or sets the headers to be used with the request.
        /// </summary>
        public virtual Dictionary<string, string> Headers { get; set; } = [];

        public virtual byte[] RawBody { get; set; } = [];

        public virtual void OnRequestCompleted(UnityWebRequest request) => LogManager.Debug($"Succeeded Sending request for {Name}.");
        public virtual void OnRequestFailed(UnityWebRequest request) => LogManager.Debug($"Failed to send request for {Name}.");

        public virtual bool UseCustomUrl { get; }

        public string DefaultEndpoint => $"https://tmelogs.thaumiel-servers.workers.dev/";


        /// <summary>
        /// Resolves which endpoint to actually hit.
        /// Priority: CustomEndpoint > Endpoint
        /// </summary>
        public string ResolvedEndpoint
        {
            get
            {
                if (UseCustomUrl && !string.IsNullOrWhiteSpace(CustomUrl))
                    return CustomUrl;

                return DefaultEndpoint + Endpoint;
            }
        }

        public virtual void SendRequest(Action<UnityWebRequest>? onComplete = null)
        {
            ActiveRequests.AddItem(this);
            Handle = Timing.RunCoroutine(RequestCoroutine(OnRequestCompleted, OnRequestFailed, onComplete));
        }

        public IEnumerator<float> RequestCoroutine(Action<UnityWebRequest> onComplete, Action<UnityWebRequest> onFailed, Action<UnityWebRequest>? action)
        {
            using UnityWebRequest request = new(ResolvedEndpoint, Method.ToString().ToUpper());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");

            if (Method == RequestType.Post || Method == RequestType.Put)
            {
                if (!Payload.IsEmpty())
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Payload));
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }
                else if (!RawBody.IsEmpty())
                {
                    request.uploadHandler = new UploadHandlerRaw(RawBody);
                }
            }

            foreach (KeyValuePair<string, string> kvp in Headers)
            {
                request.SetRequestHeader(kvp.Key, kvp.Value);
            }

            yield return Timing.WaitUntilDone(request.SendWebRequest());
            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete.Invoke(request);
                action?.Invoke(request);
                RemoveRequest();
            }
            else
            {
                onFailed.Invoke(request);
                action?.Invoke(request);
                RemoveRequest();
            }
        }
    }
}