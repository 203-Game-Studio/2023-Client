using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassBendingRenderFeature : ScriptableRendererFeature
{
    class GrassBendingRenderPass : ScriptableRenderPass {

        /*private Vector4[] positions;
        private int positionsNum;*/
        private Transform RTCamearTrans;

        public GrassBendingRenderPass(Transform RTCamearTrans) {
            //this.positions = positions;
            this.RTCamearTrans = RTCamearTrans;
        }

        //public int PositionsNum { get => this.positionsNum; set => this.positionsNum = value; }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get("GrassBending");
            //cmd.SetGlobalVectorArray("_GrassBendingPositions", positions);
            //cmd.SetGlobalInt("_GrassBendingPositionsNum", positionsNum);
            cmd.SetGlobalVector("_GrassBendingPosition", RTCamearTrans.position - new Vector3(0,5,0));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    //[SerializeField] private int maxTrackedTransforms = 8;
    public Transform RTCamearTrans;

    private GrassBendingRenderPass pass;
    //private List<Transform> trackingTransforms;
    //private Vector4[] positions;

    /*public void AddTrackedTransform(Transform transform) {
        trackingTransforms.Add(transform);
    }

    public void RemoveTrackedTransform(Transform transform) {
        trackingTransforms.Remove(transform);
    }*/

    public override void Create() {
        /*trackingTransforms = new List<Transform>();
        trackingTransforms.AddRange(FindObjectsOfType<GrassBendingObject>().Select((obj) => obj.transform));
        positions = new Vector4[maxTrackedTransforms];*/
        foreach(var camera in FindObjectsOfType<Camera>()){
            if(camera.CompareTag("RTCamera")){
                pass = new GrassBendingRenderPass(camera.transform)
                {
                    renderPassEvent = RenderPassEvent.BeforeRendering
                };
                break;
            }
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
/*#if UNITY_EDITOR
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
        pass.PositionsNum = count;*/

        renderer.EnqueuePass(pass);
    }
}
