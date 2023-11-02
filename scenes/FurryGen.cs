namespace Squigles.Furry;

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FurryGen : Node3D {
  private record ShellLayer(MeshInstance3D Mesh, int Layer, float LerpVal);



  [Export] private Node3D _rootTarget;
  [Export] private bool _searchRecursive = true;
  [Export] private float _shellHeight = 0.1f;
  [Export(PropertyHint.Range, "8,256,1,or_greater,or_less")] private float _shellCount = 16.0f;
  [Export] private int[] _surfaceTargets = Array.Empty<int>();
  [Export] private Material _furMaterial;
  [Export] private Color _furBaseColour;
  [Export] private Color _furTipColour;
  [Export] private Curve _furStiffness;
  [Export] private float _veloctyAffectFactor = 2.0f;
  [Export] private float _velocityReactionTimeAffect = 0.125f;

  private readonly List<MeshInstance3D> _furryBuffer = new();
  private readonly List<ShellLayer> _shellLayers = new();

  private Vector3 _targetVelocity;
  private Vector3 _targetLastPos;

  private const float NO_Z_FIGHTING_FACTOR = 0.99f; // prevent layers from overlapping on the resting face


  public override void _Ready() {
    _rootTarget ??= this; // assume self is the target if none selected
    LoadBuffer();
    SpawnShellLayers();
  }

  private void LoadBuffer() {
    var range = _rootTarget.FindChildren("*", "MeshInstance3D", _searchRecursive);
    _furryBuffer.AddRange(range.ToList()
      .ConvertAll((n) => n as MeshInstance3D)
      .Where((n) => n is not null).AsEnumerable());
    if (_rootTarget is MeshInstance3D m3d) {
      _furryBuffer.Add(m3d);
    }
  }

  private void SpawnShellLayers() {
    var layerDelta = _shellHeight / _shellCount;
    var count = (int)Mathf.Ceil(_shellCount);
    // GD.Print($"[Mesh={Name}] _shellHeight={_shellHeight:G2} _shellCount={_shellCount:G2} layerDelta={layerDelta} count={count}");
    foreach (var part in _furryBuffer) {
      // for each segement of the mesh that should currently have fur, add fur shell layers
      for (var i = 0; i < count; i++) {
        var depth = i * layerDelta;
        var shaderDepth = depth / _shellHeight;
        var shell = new MeshInstance3D {
          Mesh = GenerateExtrusion(part.Mesh, depth),
          MaterialOverride = _furMaterial,
          CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        // GD.Print($"[Mesh={part.Name}] Shell Layer #{i}; depth={depth} shader-depth={shaderDepth}");
        shell.SetInstanceShaderParameter("shell_depth", shaderDepth);
        shell.SetInstanceShaderParameter("base_col", _furBaseColour);
        shell.SetInstanceShaderParameter("tip_col", _furTipColour);
        part.AddChild(shell);
        _shellLayers.Add(new(shell, i, shaderDepth));
      }
    }
  }

  private Mesh GenerateExtrusion(Mesh mesh, float amount) {
    if (mesh is ArrayMesh am) {
      // absolute best option?
      return PerformExtrustion(am, amount);
    }

    var extrude = new ArrayMesh();
    for (var i = 0; i < mesh.GetSurfaceCount(); i++) {
      var arrays = mesh.SurfaceGetArrays(i);

      extrude.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    }
    return PerformExtrustion(extrude, amount);
  }
  private Mesh PerformExtrustion(ArrayMesh mesh, float amount) {
    var tool = new MeshDataTool();
    var result = new ArrayMesh();
    for (var s = 0; s < mesh.GetSurfaceCount(); s++) {
      if (_surfaceTargets.Length > 0 && !_surfaceTargets.Contains(s)) {
        // if surface not in targets, skip
        continue;
      }
      tool.CreateFromSurface(mesh, s);
      for (var v = 0; v < tool.GetVertexCount(); v++) {
        var faces = tool.GetVertexFaces(v);
        var sumNormal = new Vector3();
        foreach (var f in faces) {
          sumNormal += tool.GetFaceNormal(f);
        }
        sumNormal /= faces.Length;
        var pos = tool.GetVertex(v);
        pos += sumNormal.Normalized() * amount;
        tool.SetVertex(v, pos);
      }
      tool.CommitToSurface(result);
    }
    return result;
  }

  public override void _PhysicsProcess(double delta) {
    var posDelta = _targetLastPos - _rootTarget.GlobalPosition;
    _targetLastPos = _rootTarget.GlobalPosition;
    _targetVelocity = posDelta / (float)delta;


    var stiffnessFactor = _furStiffness;
    var layerDepth = _shellHeight / _shellCount;
    var direction = Vector3.Down + _targetVelocity;
    foreach (var shell in _shellLayers) {
      var targetPos = new Vector3();
      var offset = shell.Layer * layerDepth; // offset from base mesh
      var layerStiffness = _furStiffness.SampleBaked(shell.LerpVal);
      targetPos += direction.Normalized() * offset * Mathf.Clamp(1.0f - layerStiffness, 0, 1) * NO_Z_FIGHTING_FACTOR;
      var lerpFactor = Mathf.Clamp((1.0f - layerStiffness) * (direction.Length() * _velocityReactionTimeAffect) * (float)delta, 0.0f, .8f);
      shell.Mesh.Position = shell.Mesh.Position.Lerp(targetPos, lerpFactor);
    }
  }


}
