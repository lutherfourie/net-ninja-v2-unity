using NetNinja.Contracts;
using UnityEngine;
namespace NetNinja.View {
  public static class SnapshotToFloat {
    public static Vector3 ToVector3(Vec3 v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
  }
}
