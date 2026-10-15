// -----------------------------------------------------------------------
// <copyright file="SelectionProxyHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers
{
    public static class SelectionProxyHelper
    {
        public static SphereCollider? AddTrigger(GameObject? go, float radius = 0.5f)
        {
            if (go == null)
                return null;

            SphereCollider collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = radius;
            return collider;
        }

        public static SphereCollider? EnsureChild(GameObject? parent, float radius = 0.5f)
        {
            if (parent == null)
                return null;

            Transform existing = parent.transform.Find("[TME] Selection");
            if (existing != null && existing.TryGetComponent<SphereCollider>(out SphereCollider? found))
                return found;

            GameObject go = new("[TME] Selection");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            SphereCollider collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = radius;
            return collider;
        }

        public static void DestroyProxy(ServerObject? obj)
        {
            if (obj == null)
                return;

            GameObject? go = obj.Object;
            obj.Object = null;
            if (go == null)
                return;

            Object.Destroy(go);
        }
    }
}
