using System.Collections;
using System.IO;
using FirstLight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CaptureShots
{
    [UnityTest]
    public IEnumerator Capture()
    {
        string dir = System.Environment.GetEnvironmentVariable("FL_SHOT_DIR");
        if (string.IsNullOrEmpty(dir)) Assert.Ignore("no FL_SHOT_DIR");
        Directory.CreateDirectory(dir);
        // this is a screenshot utility, not an assertion: a cold shader library logs
        // import noise on the first render request and that must not abort the run
        LogAssert.ignoreFailingMessages = true;

        // load the shipped scene rather than building a GameManager from scratch, so
        // the shots show the art that is actually wired up
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
            "FirstLight", UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return null;
        yield return null;
        var game = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(game, "the scene should hold a GameManager");

        int[] want = { 6, 12, 29 };
        foreach (int i in want)
        {
            game.BeginAt(i);
            for (int f = 0; f < 12; f++) yield return null;

            // solve-ish poses read better than the untouched start state
            var view = Object.FindAnyObjectByType<LevelView>();
            for (int sweep = 0; sweep < 3; sweep++)
            for (int m = 0; m < view.Level.Mirrors.Count; m++)
                for (int k = 0; k < 4; k++)
                {
                    int lit = view.State.Lit.Count;
                    view.RotateMirror(m, 1);
                    if (view.State.Lit.Count > lit) break;
                }
            for (int f = 0; f < 6; f++) yield return null;

            // batchmode has no game view, so render the camera straight to a texture
            var cam = Camera.main;
            var rt = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = rt };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(cam, request))
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam, request);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, $"level{i + 1:00}.png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }

        yield return null;
    }
}
