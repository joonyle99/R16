using UnityEngine;

public class EffectParticle : EffectBase
{
    private ParticleSystem[] _particles;
    private float[] _baseScaleXs;

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>();
        _baseScaleXs = new float[_particles.Length];
        for (int i = 0; i < _particles.Length; i++)
            _baseScaleXs[i] = _particles[i].transform.localScale.x;
    }

    protected override void OnPlay()
    {
        var xSign = transform.localScale.x >= 0f ? 1f : -1f;
        for (int i = 0; i < _particles.Length; i++)
        {
            var scale = _particles[i].transform.localScale;
            _particles[i].transform.localScale = new Vector3(_baseScaleXs[i] * xSign, scale.y, scale.z);
            _particles[i].Clear();
            _particles[i].Play();
        }
    }

    protected override void OnStop()
    {
        foreach (var particle in _particles)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ParticleSystem Main 모듈의 Stop Action을 Callback으로 설정해야 호출됨
    private void OnParticleSystemStopped() => OnComplete();
}
