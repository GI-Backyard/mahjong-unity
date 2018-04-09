using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {
    // -----------------------------------------
    // DumpAnims
    // -----------------------------------------

    void DumpAnims(GameObject _animPrefab, GLTF _gltf, BufferInfo _bufInfo) {
      // get animations
      GameObject prefabInst = PrefabUtility.InstantiatePrefab(_animPrefab) as GameObject;
      List<AnimationClip> clips = Utils.GetAnimationClips(prefabInst);

      // get joints
      List<GameObject> joints = new List<GameObject>();
      Utils.RecurseNode(prefabInst, _go => {
        // this is not a joint
        if (_go.GetComponent<SkinnedMeshRenderer>() != null) {
          return false;
        }

        joints.Add(_go);
        return true;
      });

      //
      if (clips != null) {
        // process AnimationClip(s)
        foreach (AnimationClip clip in clips) {
          int accOffset = _bufInfo.GetAccessorCount();

          AnimData animData = DumpAnimData(prefabInst, clip);
          DumpBufferInfoFromAnimData(animData, _bufInfo);

          GLTF_AnimationEx gltfAnim = DumpGltfAnimationEx(animData, joints, accOffset);
          _gltf.animations.Add(gltfAnim);
        }
      }

      Object.DestroyImmediate(prefabInst);
    }

    // -----------------------------------------
    // DumpGltfAnimationEx
    // -----------------------------------------

    GLTF_AnimationEx DumpGltfAnimationEx(AnimData _animData, List<GameObject> _joints, int _accOffset) {
      GLTF_AnimationEx result = new GLTF_AnimationEx();

      result.name = _animData.name;

      List<GLTF_AnimChannelEx> channels = new List<GLTF_AnimChannelEx>();

      int offset = 2; // acc offset start from 2 (0 is time0, 1 is times)

      foreach (var entry in _animData.nameToFrames) {
        NodeFrames frames = entry.Value;

        // T
        if (frames.tlist.Count > 0) {
          // NOTE: index 0 = "time0", index 1 = "time1"
          GLTF_AnimChannelEx channel = new GLTF_AnimChannelEx {
            input = (frames.tlist.Count == 1 ? 0 : 1) + _accOffset,
            output = offset + _accOffset,
            node = _joints.IndexOf(frames.node),
            path = "translation",
          };
          channels.Add(channel);
          offset += 1;
        }

        // S
        if (frames.slist.Count > 0) {
          // NOTE: index 0 = "time0", index 1 = "time1"
          GLTF_AnimChannelEx channel = new GLTF_AnimChannelEx {
            input = (frames.slist.Count == 1 ? 0 : 1) + _accOffset,
            output = offset + _accOffset,
            node = _joints.IndexOf(frames.node),
            path = "scale",
          };
          channels.Add(channel);
          offset += 1;
        }

        // R
        if (frames.rlist.Count > 0) {
          // NOTE: index 0 = "time0", index 1 = "time1"
          GLTF_AnimChannelEx channel = new GLTF_AnimChannelEx {
            input = (frames.rlist.Count == 1 ? 0 : 1) + _accOffset,
            output = offset + _accOffset,
            node = _joints.IndexOf(frames.node),
            path = "rotation",
          };
          channels.Add(channel);
          offset += 1;
        }
      }

      result.channels = channels;

      return result;
    }

    // -----------------------------------------
    // DumpGltfAnimation
    // -----------------------------------------

    GLTF_Animation DumpGltfAnimation(AnimData _animData, List<GameObject> _joints, int _accOffset) {
      GLTF_Animation result = new GLTF_Animation();

      result.name = _animData.name;

      List<GLTF_AnimChannel> channels = new List<GLTF_AnimChannel>();
      List<GLTF_AnimSampler> samplers = new List<GLTF_AnimSampler>();

      int offset = 2; // acc offset start from 2 (0 is time0, 1 is times)

      foreach (var entry in _animData.nameToFrames) {
        NodeFrames frames = entry.Value;

        // T
        if (frames.tlist.Count > 0) {
          GLTF_AnimChannel channel = new GLTF_AnimChannel {
            sampler = samplers.Count,
            target = new GLTF_AnimTarget {
              node = _joints.IndexOf(frames.node),
              path = "translation",
            }
          };
          channels.Add(channel);

          // NOTE: index 0 = "time0", index 1 = "time1"
          GLTF_AnimSampler sampler = new GLTF_AnimSampler {
            input = (frames.tlist.Count == 1 ? 0 : 1) + _accOffset,
            output = offset + _accOffset,
          };

          samplers.Add(sampler);
          offset += 1;
        }

        // S
        if (frames.slist.Count > 0) {
          GLTF_AnimChannel channel = new GLTF_AnimChannel {
            sampler = samplers.Count,
            target = new GLTF_AnimTarget {
              node = _joints.IndexOf(frames.node),
              path = "scale",
            }
          };
          channels.Add(channel);

          // NOTE: index 0 = "time0", index 1 = "time1"
          GLTF_AnimSampler sampler = new GLTF_AnimSampler {
            input = (frames.tlist.Count == 1 ? 0 : 1) + _accOffset,
            output = offset + _accOffset,
          };

          samplers.Add(sampler);
          offset += 1;
        }

        // R
        if (frames.rlist.Count > 0) {
          GLTF_AnimChannel channel = new GLTF_AnimChannel {
            sampler = samplers.Count,
            target = new GLTF_AnimTarget {
              node = _joints.IndexOf(frames.node),
              path = "rotation",
            }
          };
          channels.Add(channel);

          // NOTE: index 0 = "time0", index 1 = "time1"
          GLTF_AnimSampler sampler = new GLTF_AnimSampler {
            input = (frames.tlist.Count == 1 ? 0 : 1) + _accOffset,
            output = offset + _accOffset,
          };

          samplers.Add(sampler);
          offset += 1;
        }
      }

      result.channels = channels;
      result.samplers = samplers;

      return result;
    }

    // -----------------------------------------
    // DumpAnimData
    // -----------------------------------------

    AnimData DumpAnimData(GameObject _prefabInst, AnimationClip _clip) {
      AnimData animData = new AnimData();

      // name
      animData.name = _clip.name;

      // get frames
      float step = 1.0f / _clip.frameRate;
      for (float t = 0.0f; t < _clip.length; t += step) {
        animData.times.Add(t);
      }
      animData.times.Add(_clip.length);

      // sample frames
      for (int i = 0; i < animData.times.Count; ++i) {
        float t = animData.times[i];
        _clip.SampleAnimation(_prefabInst, t);

        Utils.RecurseNode(_prefabInst, _go => {
          // this is not a joint
          if (_go.GetComponent<SkinnedMeshRenderer>() != null) {
            return false;
          }

          // dump translation, scale and rotation in current frame
          string name = _go.name;
          NodeFrames frames;

          if (animData.nameToFrames.TryGetValue(name, out frames) == false) {
            frames = new NodeFrames();
            frames.node = _go;

            animData.nameToFrames.Add(name, frames);
          }

          frames.tlist.Add(_go.transform.localPosition);
          frames.slist.Add(_go.transform.localScale);
          frames.rlist.Add(_go.transform.localRotation);

          return true;
        });
      }

      // strip empty frames (keep 1 frame for it)
      foreach (var entry in animData.nameToFrames) {
        NodeFrames frames = entry.Value;

        // T
        bool hasFrameT = false;
        for (int i = 0; i < frames.tlist.Count; ++i) {
          if (frames.tlist[i] != frames.tlist[0]) {
            hasFrameT = true;
            break;
          }
        }
        if (hasFrameT == false) {
          if (frames.tlist[0] == Vector3.zero) {
            frames.tlist.Clear();
          } else {
            frames.tlist.RemoveRange(1, frames.tlist.Count - 1);
            hasFrameT = true;
          }
        }

        // S
        bool hasFrameS = false;
        for (int i = 0; i < frames.slist.Count; ++i) {
          if (frames.slist[i] != frames.slist[0]) {
            hasFrameS = true;
            break;
          }
        }
        if (hasFrameS == false) {
          if (frames.slist[0] == Vector3.one) {
            frames.slist.Clear();
          } else {
            frames.slist.RemoveRange(1, frames.slist.Count - 1);
            hasFrameS = true;
          }
        }

        // R
        bool hasFrameR = false;
        for (int i = 0; i < frames.rlist.Count; ++i) {
          if (frames.rlist[i] != frames.rlist[0]) {
            hasFrameR = true;
            break;
          }
        }
        if (hasFrameR == false) {
          if (frames.rlist[0] == Quaternion.identity) {
            frames.rlist.Clear();
          } else {
            frames.rlist.RemoveRange(1, frames.rlist.Count - 1);
            hasFrameR = true;
          }
        }

        //
        if (!hasFrameT && !hasFrameS && !hasFrameR) {
          animData.nameToFrames.Remove(name);
        }
      }

      return animData;
    }

    // -----------------------------------------
    // DumpBufferInfoFromAnimData
    // -----------------------------------------

    void DumpBufferInfoFromAnimData(AnimData _animData, BufferInfo _bufInfo) {
      List<AccessorInfo> accessors = new List<AccessorInfo>();

      // calculate total length of animation-data
      int length = 0;
      AccessorInfo acc;

      // time0
      acc = new AccessorInfo
      {
        name = "time0@" + _animData.name,
        offset = length,
        count = 1,
        compType = ComponentType.FLOAT32,
        attrType = AttrType.SCALAR
      };

      accessors.Add(acc);
      length += 4;

      // times
      acc = new AccessorInfo
      {
        name = "times@" + _animData.name,
        offset = length,
        count = _animData.times.Count,
        compType = ComponentType.FLOAT32,
        attrType = AttrType.SCALAR
      };

      accessors.Add(acc);
      length += _animData.times.Count * 4;

      // frames
      foreach (var entry in _animData.nameToFrames) {
        NodeFrames frames = entry.Value;

        if (frames.tlist.Count > 0) {
          acc = new AccessorInfo
          {
            name = frames.node.name + "_T@" + _animData.name,
            offset = length,
            count = frames.tlist.Count,
            compType = ComponentType.FLOAT32,
            attrType = AttrType.VEC3
          };

          accessors.Add(acc);
          length += frames.tlist.Count * 12;
        }

        if (frames.slist.Count > 0) {
          acc = new AccessorInfo
          {
            name = frames.node.name + "_S@" + _animData.name,
            offset = length,
            count = frames.slist.Count,
            compType = ComponentType.FLOAT32,
            attrType = AttrType.VEC3
          };

          accessors.Add(acc);
          length += frames.slist.Count * 12;
        }

        if (frames.rlist.Count > 0) {
          acc = new AccessorInfo
          {
            name = frames.node.name + "_R@" + _animData.name,
            offset = length,
            count = frames.rlist.Count,
            compType = ComponentType.FLOAT32,
            attrType = AttrType.VEC4
          };

          accessors.Add(acc);
          length += frames.rlist.Count * 16;
        }
      }

      // write data
      byte[] clipData;
      using (MemoryStream stream = new MemoryStream(length)) {
        using (BinaryWriter writer = new BinaryWriter(stream)) {
          // time0
          writer.Write(0.0f);

          // times
          for (int i = 0; i < _animData.times.Count; ++i) {
            writer.Write(_animData.times[i]);
          }

          foreach (var entry in _animData.nameToFrames) {
            NodeFrames frames = entry.Value;

            if (frames.tlist.Count > 0) {
              for (int i = 0; i < frames.tlist.Count; ++i) {
                // NOTE: convert LH to RH
                writer.Write(frames.tlist[i].x);
                writer.Write(frames.tlist[i].y);
                writer.Write(-frames.tlist[i].z);
              }
            }

            if (frames.slist.Count > 0) {
              for (int i = 0; i < frames.slist.Count; ++i) {
                writer.Write(frames.slist[i].x);
                writer.Write(frames.slist[i].y);
                writer.Write(frames.slist[i].z);
              }
            }

            if (frames.rlist.Count > 0) {
              for (int i = 0; i < frames.rlist.Count; ++i) {
                // NOTE: convert LH to RH
                writer.Write(-frames.rlist[i].x);
                writer.Write(-frames.rlist[i].y);
                writer.Write(frames.rlist[i].z);
                writer.Write(frames.rlist[i].w);
              }
            }
          }
        }
        clipData = stream.ToArray();
      }

      // buffer view
      BufferViewInfo bufView = new BufferViewInfo
      {
        name = _animData.name,
        offset = _bufInfo.data.Length,
        length = clipData.Length,
        type = BufferType.NONE,
        accessors = accessors
      };

      //
      byte[] data = new byte[_bufInfo.data.Length + clipData.Length];
      int offset = 0;

      System.Buffer.BlockCopy(_bufInfo.data, 0, data, offset, _bufInfo.data.Length);
      offset += _bufInfo.data.Length;

      System.Buffer.BlockCopy(clipData, 0, data, offset, clipData.Length);
      offset += clipData.Length;

      //
      _bufInfo.data = data;
      _bufInfo.bufferViews.Add(bufView);
    }
  }
}