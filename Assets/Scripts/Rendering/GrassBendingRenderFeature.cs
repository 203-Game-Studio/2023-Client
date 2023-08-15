using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassBendingRenderFeature : ScriptableRendererFeature
{
    class GrassBendingRenderPass : ScriptableRenderPass {

        private Vector4[] positions;
        private int positionsNum;

        public GrassBendingRenderPass(Vector4[] positions) {
            this.positions = positions;
        }

        public int PositionsNum { get => this.positionsNum; set => this.positionsNum = value; }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get("GrassTrampleFeature");
            cmd.SetGlobalVectorArray("_GrassBendingPositions", positions);
            cmd.SetGlobalInt("_GrassBendingPositionsNum", positionsNum);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [SerializeField] private int maxTrackedTransforms = 8;

    private GrassBendingRenderPass pass;
    private List<Transform> trackingTransforms;
    private Vector4[] positions;

    public void AddTrackedTransform(Transform transform) {
        trackingTransforms.Add(transform);
    }

    public void RemoveTrackedTransform(Transform transform) {
        trackingTransforms.Remove(transform);
    }

    public override void Create() {
        trackingTransforms = new List<Transform>();
        trackingTransforms.AddRange(FindObjectsOfType<GrassBendingObject>().Select((obj) => obj.transform));
        positions = new Vector4[maxTrackedTransforms];
        pass = new GrassBendingRenderPass(positions)
        {
            renderPassEvent = RenderPassEvent.BeforeRendering
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
#if UNITY_EDITOR
        trackingTransforms.RemoveAll((t) => t == null);
#endif

        for(int i = 0; i < positions.Length; i++) {
            positions[i] = Vector4.zero;
        }
        int count = (int)Mathf.Min(trackingTransforms.Count, positions.Length);
        for(int i = 0; i < count; i++) {
            Vector3 pos = trackingTransforms[i].position;
            positions[i] = new Vector4(pos.x, pos.y, pos.z, 1);
        }
        pass.PositionsNum = count;

        renderer.EnqueuePass(pass);
    }
}
