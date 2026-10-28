using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundMgr : UnitySingleton<SoundMgr>
{
    private const int MAX_SOUNDS = 8; // 同时播放8个音效;
    private const string MusicMuteKey = "isMusicMute";
    private const string SoundMuteKey = "isSoundMute";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SoundVolumeKey = "SoundVolume";

    private List<AudioSource> sounds;
    private int curIndex; // 当前使用的第几个AudioSource来播放;
    private AudioSource musicSource = null;

    private int isMusicMute = 0;
    private int isSoundMute = 0;

    public void Init() {
        this.sounds = new List<AudioSource>();

        for (int i = 0; i < MAX_SOUNDS; i++) {
            AudioSource audioSource = this.gameObject.AddComponent<AudioSource>();
            this.sounds.Add(audioSource);
        }

        this.musicSource = this.gameObject.AddComponent<AudioSource>();
        this.curIndex = 0;

        this.isMusicMute = 0;
        if (PlayerPrefs.HasKey(MusicMuteKey)) {
            this.isMusicMute = PlayerPrefs.GetInt(MusicMuteKey);
        }
        

        this.isSoundMute = 0;
        if (PlayerPrefs.HasKey(SoundMuteKey)) {
            this.isSoundMute = PlayerPrefs.GetInt(SoundMuteKey);
        }

        float soundVolume = 1.0f;
        if(PlayerPrefs.HasKey(SoundVolumeKey)) {
            soundVolume = PlayerPrefs.GetFloat(SoundVolumeKey);
        }
        this.SetSoundVolume(soundVolume);

        float musicVolume = 1.0f;
        if (PlayerPrefs.HasKey(MusicVolumeKey)) {
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey);
        }
        this.SetMusicVolume(musicVolume);

    }

    public void PlayMusic(string musicName, bool loop = true) {
        AudioClip clip = ResMgr.Instance.LoadAssetSync<AudioClip>(musicName);
        if (clip == null) {
            return;
        }

        this.musicSource.clip = clip;
        this.musicSource.loop = loop;

        if (this.isMusicMute != 0) {
            return;
        }
        this.musicSource.Play();
    }

    public void SlopMusic() {
        this.musicSource.Stop();
    }

    public int PlaySound(string soundName, bool loop = false) {
        if (this.isSoundMute != 0) {
            return -1;
        }
        
        AudioClip clip = ResMgr.Instance.LoadAssetSync<AudioClip>(soundName);
        if (clip == null) {
            return -1;
        }

        int soundId = this.curIndex;    
        AudioSource audioSource = this.sounds[this.curIndex];
        this.curIndex++;
        this.curIndex = (this.curIndex >= this.sounds.Count) ? 0 : this.curIndex;

        audioSource.clip = clip;
        audioSource.loop = loop;

        if (this.isSoundMute != 0) {
            return soundId;
        }

        audioSource.Play();

        return soundId;
    }

    public int PlayOneShot(string soundName, bool loop = false) {
        if (this.isSoundMute != 0) {
            return -1;
        }

        AudioClip clip = ResMgr.Instance.LoadAssetSync<AudioClip>(soundName);
        if (clip == null) {
            return -1;
        }

        int soundId = this.curIndex;
        AudioSource audioSource = this.sounds[this.curIndex];
        this.curIndex++;
        this.curIndex = (this.curIndex >= this.sounds.Count) ? 0 : this.curIndex;

        audioSource.clip = clip;
        audioSource.loop = loop;

        if (this.isSoundMute != 0) {
            return soundId;
        }

        audioSource.PlayOneShot(clip);

        return soundId;
    }

    public void StopSound(int soundId) {
        if (soundId < 0 || soundId >= this.sounds.Count) {
            return;
        }

        AudioSource audioSource = this.sounds[soundId];
        audioSource.Stop();
    }

    public void StopAllSound() {
        for (int i = 0; i < this.sounds.Count; i++) {
            AudioSource audioSource = this.sounds[i];
            audioSource.Stop();
        }
    }

    public void SetMusicMute(bool isMute) {
        bool isMuscMute = (this.isMusicMute != 0);
        if (isMuscMute == isMute) {
            return;
        }

        this.isMusicMute = isMute ? 1 : 0;
        PlayerPrefs.SetInt(MusicMuteKey, this.isMusicMute);
        this.musicSource.mute = isMute;

    }

    public void SetSoundMute(bool isMute) {
        bool isSoundMute = (this.isSoundMute != 0);
        if (isSoundMute == isMute) {
            return;
        }

        this.isSoundMute = isMute ? 1 : 0;
        PlayerPrefs.SetInt(SoundMuteKey, this.isSoundMute);

        // 音效都是短暂的，所以我们这里就不管它了;
        for (int i = 0; i < this.sounds.Count; i++) {
            this.sounds[i].mute = isMute;
        }
        // end 
    }

    public void SetMusicVolume(float per) {
        per = Mathf.Clamp(per, 0, 1);
        this.musicSource.volume = per;

        PlayerPrefs.SetFloat(MusicVolumeKey, per);
    }

    public void SetSoundVolume(float per) {
        per = Mathf.Clamp(per, 0, 1);
        for (int i = 0; i < this.sounds.Count; i++) {
            this.sounds[i].volume = per;
        }

        PlayerPrefs.SetFloat(SoundVolumeKey, per);
    }
}
