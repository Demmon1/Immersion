﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Immersion
{
    public class PestleRenderer : IRenderer
    {
        internal bool ShouldRender;
        internal bool ShouldRotate;

        private ICoreClientAPI Api;

        public BlockPos Pos { get; private set; }

        MeshRef meshref;
        public Matrixf ModelMat = new Matrixf();

        public float Angle;

        public virtual float SoundLevel
        {
            get { return 0.5f; }
        }

        public PestleRenderer(ICoreClientAPI coreClientAPI, BlockPos Pos, MeshData mesh)
        {
            Api = coreClientAPI;
            this.Pos = Pos;
            MeshRef test = new MeshRef();

            meshref = coreClientAPI.Render.UploadMesh(mesh);
            
        }

        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange => 24;

        float xf = 0.0f;
        float yf = -0.5f;
        float zf = 0.0f;
        bool yb = true;
        bool dO = true;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshref == null || !ShouldRender) return;

            
            IRenderAPI rpi = Api.Render;
            Vec3d camPos = Api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(Pos.X, Pos.Y, Pos.Z);
            prog.Tex2D = Api.BlockTextureAtlas.AtlasTextureIds[0];


            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y, Pos.Z - camPos.Z)
                .Translate(0.5f, 11f / 16f, 0.5f)
                .RotateY(Angle)
                .Translate(-0.5f, 0, -0.5f)
                .Translate(xf, yf, zf)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshref);
            prog.Stop();



            if (ShouldRotate)
            {
                if (yb && yf <= 0.5f)
                {
                    float jl = Convert.ToSingle(Api.World.Rand.NextDouble());
                    xf += deltaTime * 0.02f;
                    yf += deltaTime * jl * 5.0f;
                    zf += deltaTime * 0.02f;
                }
                else
                {
                    yb = false;
                    if (dO && Api.Side == EnumAppSide.Client) {
                        Api.World.PlaySoundAt(Api.World.BlockAccessor.GetBlock(new AssetLocation("game:gravel-andesite")).Sounds.Break, Pos.X, Pos.Y, Pos.Z);
                        dO = false;
                    }
                }
                if (!yb && yf >= -0.2f)
                {
                    float jl = Convert.ToSingle(Api.World.Rand.NextDouble());
                    xf -= deltaTime * 0.04f;
                    yf -= deltaTime * jl * 10.0f;
                    zf -= deltaTime * 0.04f;
                }
                else { yb = true; dO = true; }
                if (!yb) Angle += (deltaTime * 100) * GameMath.DEG2RAD;
                if (Angle * GameMath.RAD2DEG > 360.0f) Angle = 0.0f * GameMath.DEG2RAD;
            }
            else
            {
                xf = 0.0f;
                yf = 0.0f;
                zf = 0.0f;
                Angle = 0.0f * GameMath.DEG2RAD;
            }
        }


        internal void Unregister()
        {
            Api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void Dispose()
        {
            meshref.Dispose();
        }


    }
}
