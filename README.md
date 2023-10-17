![Unity icon](https://images.contentstack.io/v3/assets/blt08c1239a7bff8ff5/bltdff1a2920dd347a5/63f5068a97790d11728d0a6d/U_Logo_Small_black.svg)

<h2 align="center">
SMPL character animation in Unity

with real-time estimated 3D pose from a single monocular RGB image
</h2>

------------

# About

There are so many models estimating 3D human mesh with shape reconstruction using smpl-like model.
[Expose](https://github.com/vchoutas/expose), [VIBE](https://github.com/mkocabas/VIBE), 
[HuManiFlow](https://github.com/akashsengupta1997/HuManiFlow) are some of those examples. 
Sometimes, they could reconstruct human pose in real-time quite well.
But it is hard to import those on Unity because of the onnx-barracuda problem.
Even if it is possible, the accuracy drops when converting python model to onnx model, and might be hard to be conducted in real-time.

So, we decided to split it as two sub modules, shape reconstruction and pose estimation.
Shape reconstruction is done by a python server. When client(Unity side) requests to get current frame's human shape information, byte-converted frame image is sent to the server and waiting for a response asynchronously.
When server receives image data, it would be cropped or padded to the desired size for its own Smpl-like mesh inference model(we used [HuManiFlow](https://github.com/akashsengupta1997/HuManiFlow) for demo) and forwarded to that model. 
After inference, shape parameters(betas) are packed into response message , sent to client, and connection is closed.

Pose estimation is done by Unity itself. We selected [MediaPipe](https://developers.google.com/mediapipe) for pose estimation([MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin); Pre-developed Plugin are there already).
Thanks to [homuler](https://ko-fi.com/homuler)(the author of the plugin), we could get 3d pose estimated data without networking.


--------------

# Architecture
Here is an architecture image.
![architecture.png](readmeImg%2Farchitecture.png)

-------------

# Shape Reconstruction

### Note that
You have to maintain your own Smplx Reconstruction Server, if you want to set Smplx body shapes automatically. Those two endpoint would be connected  by TCP.

You can make .txt file containing a hostname(ipv4 addr) and a port number of shape inference server, and locate it ``./Assets/RealTimeSMPL/ShapeConf/Secrets/`` directory.
By assigning it to UnitySocketCleint_auto.cs script's IpConf variable,
it will send an image frame, receive betas and pass it to the target smplx mesh sequentially.

The script is located at

    Main Canvas > ContainerPanel > Body > Annotatable Screen
in the Assets > RealTimeSMPL > Scene > RealTimeSMPLX.unity scene's Hierarchy window.

Hostname and port number must be split by "line separator" like this
```text
xxx.xxx.xx.xxx
0000
```

### API specification

- request

![req.png](readmeImg%2Freq.png)

- response

![res.png](readmeImg%2Fres.png)

Those two byte arrays are passed on tcp stream. Request form would be passed on the stream correctly if you assigned valid value on IpConf variable.
You have to obey response message format when implementing inference server.

-------------

# Pose Estimation

We used [MediaPipe](https://developers.google.com/mediapipe) framework for 3D pose estimation. 

![mediaPipeJoints.png](readmeImg%2FmediaPipeJoints.png)

The estimated data contains each joint's rotation angles. 
Since the skeleton structure and the number of joints in the MediaPipe framework were different from those in SMPL/SMPLX, it was necessary to work on mapping them as closely as possible.

![image](https://github.com/sangho0n/Realtime_SMPLX_Unity/assets/54069713/dabdaad8-e118-4ea0-8826-d5baed88d245)


------------

# Demo
You can find demo scene at ``./RealTimeSMPL/Scene/RealTimeSMPLX``. Run the scene and see your smpl avatar dancing just like you.

If you constructed your own smpl human mesh reconstruction server and configured connection correctly, the smpl mesh's shape changes every time you push down space bar or right-click your mouse.

## Get Repository
This repository does not contain required libraries (e.g. libmediapipe_c.so, Google.Protobuf.dll, etc). 
You have to download whole things at [this page]() and extract it anywhere you want, instead of ``git clone``.

// TODO upload an actual download site.

## Tested Environment

This repository is based on...
- Unity 2021.3.18
- [MediaPipeUnityPlugin_v0.11.0](https://github.com/homuler/MediaPipeUnityPlugin/releases)


and tested on 

- OS : Ubuntu 20.04, Windows 10
- Processor : AMD Ryzen 7 5800X + RTX 3080 Ti / AMD Ryzen 5 4500U with Radeon Graphics

[Demo video Youtube link](https://www.youtube.com/watch?v=Tq7Mzuc6t6M)

------------

# Limitation
Despite such efforts, it still exhibited less natural movements when inferring and rendering the joints of SMPL/SMPLX meshes in real-time in Python, as shown in the demo video linked below.
If we can find a way to perform real-time Pose Estimation in Unity that aligns with the skeleton structure used in SMPL, it is likely to yield better results.


-------
# References

- [MediaPipeUnityPlugin github repository](https://github.com/homuler/MediaPipeUnityPlugin)
- [smplx github repository](https://github.com/vchoutas/smplx)
- [rigging and animation code reference](https://github.com/digital-standard/ThreeDPoseUnityBarracuda)

<details>
	<summary>Original README from MediaPipeUnityPlugin repository</summary>
	<div markdown="1">


# MediaPipe Unity Plugin

This is a Unity (2021.3.18f1) [Native Plugin](https://docs.unity3d.com/Manual/NativePlugins.html) to use [MediaPipe](https://github.com/google/mediapipe) (0.9.1).

The goal of this project is to port the MediaPipe API (C++) _one by one_ to C# so that it can be called from Unity.\
This approach may sacrifice performance when you need to call multiple APIs in a loop, but it gives you the flexibility to use MediaPipe instead.

With this plugin, you can

- Write MediaPipe code in C#.
- Run MediaPipe's official solution on Unity.
- Run your custom `Calculator` and `CalculatorGraph` on Unity.
    - :warning: Depending on the type of input/output, you may need to write C++ code.

## :smile_cat: Hello World!

Here is a Hello World! example.\
Compare it with [the official code](https://github.com/google/mediapipe/blob/cf101e62a9d49a51be76836b2b8e5ba5c06b5da0/mediapipe/examples/desktop/hello_world/hello_world.cc)!

```cs
using Mediapipe;
using UnityEngine;

public sealed class HelloWorld : MonoBehaviour
{
    private const string _ConfigText = @"
input_stream: ""in""
output_stream: ""out""
node {
  calculator: ""PassThroughCalculator""
  input_stream: ""in""
  output_stream: ""out1""
}
node {
  calculator: ""PassThroughCalculator""
  input_stream: ""out1""
  output_stream: ""out""
}
";

    private void Start()
    {
        var graph = new CalculatorGraph(_ConfigText);
        var poller = graph.AddOutputStreamPoller<string>("out").Value();
        graph.StartRun().AssertOk();

        for (var i = 0; i < 10; i++)
        {
            graph.AddPacketToInputStream("in", new StringPacket("Hello World!", new Timestamp(i))).AssertOk();
        }

        graph.CloseInputStream("in").AssertOk();
        var packet = new StringPacket();

        while (poller.Next(packet))
        {
            Debug.Log(packet.Get());
        }
        graph.WaitUntilDone().AssertOk();
    }
}
```

For more detailed usage, see [the API Overview](https://github.com/homuler/MediaPipeUnityPlugin/wiki/API-Overview) page or the tutorial on [the Getting Started page](https://github.com/homuler/MediaPipeUnityPlugin/wiki/Getting-Started).

## :hammer_and_wrench: Installation

This repository **does not contain required libraries** (e.g. `libmediapipe_c.so`, `Google.Protobuf.dll`, etc).\
You can download them from [the release page](https://github.com/homuler/MediaPipeUnityPlugin/releases) instead.

|                 file                  |                                                      contents                                                      |
| :-----------------------------------: | :----------------------------------------------------------------------------------------------------------------: |
|    `MediaPipeUnityPlugin-all.zip`     | All the source code with required libraries. If you need to run sample scenes on your mobile devices, prefer this. |
| `com.github.homuler.mediapipe-*.tgz`  |                      [A tarball package](https://docs.unity3d.com/Manual/upm-ui-tarball.html)                      |
| `MediaPipeUnityPlugin.*.unitypackage` |                                               A `.unitypackage` file                                               |

If you want to customize the package or minify the package size, you need to build them by yourself.\
For a step-by-step guide, please refer to the [Installation Guide](https://github.com/homuler/MediaPipeUnityPlugin/wiki/Installation-Guide) on Wiki.\
You can also make use of [the Package Workflow](https://github.com/homuler/MediaPipeUnityPlugin/blob/master/.github/workflows/package.yml) on Github Actions after forking this repository.

> :warning: libraries that can be built differ depending on your environment.

### Supported Platforms

> :warning: GPU mode is not supported on macOS and Windows.

|                            |       Editor       |   Linux (x86_64)   |   macOS (x86_64)   |   macOS (ARM64)    |  Windows (x86_64)  |      Android       |        iOS         | WebGL |
| :------------------------: | :----------------: | :----------------: | :----------------: | :----------------: | :----------------: | :----------------: | :----------------: | :---: |
|     Linux (AMD64) [^1]     | :heavy_check_mark: | :heavy_check_mark: |                    |                    |                    | :heavy_check_mark: |                    |       |
|         Intel Mac          | :heavy_check_mark: |                    | :heavy_check_mark: |                    |                    | :heavy_check_mark: | :heavy_check_mark: |       |
|           M1 Mac           | :heavy_check_mark: |                    |                    | :heavy_check_mark: |                    | :heavy_check_mark: | :heavy_check_mark: |       |
| Windows 10/11 (AMD64) [^2] | :heavy_check_mark: |                    |                    |                    | :heavy_check_mark: | :heavy_check_mark: |                    |       |

[^1]: Tested on Arch Linux.
[^2]: Running MediaPipe on Windows is [experimental](https://google.github.io/mediapipe/getting_started/install.html#installing-on-windows).

## :plate_with_cutlery: Try the sample app

### Example Solutions

Here is a list of [solutions](https://google.github.io/mediapipe/solutions/solutions.html) that you can try in the sample app.

> :bell: The graphs you can run are not limited to the ones in this list.

|                         |      Android       |        iOS         |    Linux (GPU)     |    Linux (CPU)     |    macOS (CPU)     |   Windows (CPU)    | WebGL |
| :---------------------: | :----------------: | :----------------: | :----------------: | :----------------: | :----------------: | :----------------: | ----- |
|     Face Detection      | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|        Face Mesh        | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|          Iris           | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|          Hands          | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|          Pose           | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|        Holistic         | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|   Selfie Segmentation   | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|    Hair Segmentation    | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|    Object Detection     | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|      Box Tracking       | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
| Instant Motion Tracking | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|        Objectron        | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |       |
|          KNIFT          |                    |                    |                    |                    |                    |                    |       |

### UnityEditor

Select `Mediapipe/Samples/Scenes/Start Scene` and play.

### Desktop

If you've built native libraries for CPU (i.e. `--desktop cpu`), select `CPU` for inference mode from the Inspector Window.
![preferable-inference-mode](https://user-images.githubusercontent.com/4690128/134795568-156f3d41-b46e-477f-a487-d04c99300c33.png)

### Android, iOS

Make sure that you select `GPU` for inference mode before building the app, because `CPU` inference mode is not supported currently.

## :book: Wiki

https://github.com/homuler/MediaPipeUnityPlugin/wiki

## :scroll: LICENSE

[MIT](https://github.com/homuler/MediaPipeUnityPlugin/blob/master/LICENSE)

Note that some files are distributed under other licenses.

- MediaPipe ([Apache Licence 2.0](https://github.com/google/mediapipe/blob/e6c19885c6d3c6f410c730952aeed2852790d306/LICENSE))
- emscripten ([MIT](https://github.com/emscripten-core/emscripten/blob/7c873832e933e86855f5ef5f7c6438f0e457c94e/LICENSE))
    - `third_party/mediapipe_emscripten_patch.diff` contains code copied from emscripten
- FontAwesome ([LICENSE](https://github.com/FortAwesome/Font-Awesome/blob/7cbd7f9951be31f9d06b6ac97739a700320b9130/LICENSE.txt))
    - Sample scenes use Font Awesome fonts

See also [Third Party Notices.md](https://github.com/homuler/MediaPipeUnityPlugin/blob/master/Third%20Party%20Notices.md).

	</div>
</details>

<details>	
<summary>Original README from smplx repository</summary>
	<div markdown="1">

## SMPL-X:  A new joint 3D model of the human body, face and hands together

[[Paper Page](https://smpl-x.is.tue.mpg.de)] [[Paper](https://ps.is.tuebingen.mpg.de/uploads_file/attachment/attachment/497/SMPL-X.pdf)]
[[Supp. Mat.](https://ps.is.tuebingen.mpg.de/uploads_file/attachment/attachment/498/SMPL-X-supp.pdf)]

![SMPL-X Examples](https://github.com/vchoutas/smplx/raw/main/images/teaser_fig.png)

## Table of Contents
* [License](#license)
* [Description](#description)
* [News](#news)
* [Installation](#installation)
* [Downloading the model](#downloading-the-model)
* [Loading SMPL-X, SMPL+H and SMPL](#loading-smpl-x-smplh-and-smpl)
    * [SMPL and SMPL+H setup](#smpl-and-smplh-setup)
    * [Model loading](https://github.com/vchoutas/smplx#model-loading)
* [MANO and FLAME correspondences](#mano-and-flame-correspondences)
* [Example](#example)
* [Modifying the global pose of the model](#modifying-the-global-pose-of-the-model)
* [Citation](#citation)
* [Acknowledgments](#acknowledgments)
* [Contact](#contact)

## License

Software Copyright License for **non-commercial scientific research purposes**.
Please read carefully the [terms and conditions](https://github.com/vchoutas/smplx/blob/master/LICENSE) and any accompanying documentation before you download and/or use the SMPL-X/SMPLify-X model, data and software, (the "Model & Software"), including 3D meshes, blend weights, blend shapes, textures, software, scripts, and animations. By downloading and/or using the Model & Software (including downloading, cloning, installing, and any other use of this github repository), you acknowledge that you have read these terms and conditions, understand them, and agree to be bound by them. If you do not agree with these terms and conditions, you must not download and/or use the Model & Software. Any infringement of the terms of this agreement will automatically terminate your rights under this [License](./LICENSE).

## Disclaimer

The original images used for the figures 1 and 2 of the paper can be found in this link.
The images in the paper are used under license from gettyimages.com.
We have acquired the right to use them in the publication, but redistribution is not allowed.
Please follow the instructions on the given link to acquire right of usage.
Our results are obtained on the 483 × 724 pixels resolution of the original images.

## Description

*SMPL-X* (SMPL eXpressive) is a unified body model with shape parameters trained jointly for the
face, hands and body. *SMPL-X* uses standard vertex based linear blend skinning with learned corrective blend
shapes, has N = 10, 475 vertices and K = 54 joints,
which include joints for the neck, jaw, eyeballs and fingers.
SMPL-X is defined by a function M(θ, β, ψ), where θ is the pose parameters, β the shape parameters and
ψ the facial expression parameters.

## News

- 3 November 2020: We release the code to transfer between the models in the
  SMPL family. For more details on the code, go to this [readme
  file](./transfer_model/README.md). A detailed explanation on how the mappings
  were extracted can be found [here](./transfer_model/docs/transfer.md).
- 23 September 2020: A UV map is now available for SMPL-X, please check the
  Downloads section of the website.
- 20 August 2020: The full shape and expression space of SMPL-X are now available.

## Installation

To install the model please follow the next steps in the specified order:
1. To install from PyPi simply run:
  ```Shell
  pip install smplx[all]
  ```
2. Clone this repository and install it using the *setup.py* script:
```Shell
git clone https://github.com/vchoutas/smplx
python setup.py install
```

## Downloading the model

To download the *SMPL-X* model go to [this project website](https://smpl-x.is.tue.mpg.de) and register to get access to the downloads section.

To download the *SMPL+H* model go to [this project website](http://mano.is.tue.mpg.de) and register to get access to the downloads section.

To download the *SMPL* model go to [this](http://smpl.is.tue.mpg.de) (male and female models) and [this](http://smplify.is.tue.mpg.de) (gender neutral model) project website and register to get access to the downloads section.

## Loading SMPL-X, SMPL+H and SMPL

### SMPL and SMPL+H setup

The loader gives the option to use any of the SMPL-X, SMPL+H, SMPL, and MANO models. Depending on the model you want to use, please follow the respective download instructions. To switch between MANO, SMPL, SMPL+H and SMPL-X just change the *model_path* or *model_type* parameters. For more details please check the docs of the model classes.
Before using SMPL and SMPL+H you should follow the instructions in [tools/README.md](./tools/README.md) to remove the
Chumpy objects from both model pkls, as well as merge the MANO parameters with SMPL+H.

### Model loading

You can either use the [create](https://github.com/vchoutas/smplx/blob/c63c02b478c5c6f696491ed9167e3af6b08d89b1/smplx/body_models.py#L54)
function from [body_models](./smplx/body_models.py) or directly call the constructor for the
[SMPL](https://github.com/vchoutas/smplx/blob/c63c02b478c5c6f696491ed9167e3af6b08d89b1/smplx/body_models.py#L106),
[SMPL+H](https://github.com/vchoutas/smplx/blob/c63c02b478c5c6f696491ed9167e3af6b08d89b1/smplx/body_models.py#L395) and
[SMPL-X](https://github.com/vchoutas/smplx/blob/c63c02b478c5c6f696491ed9167e3af6b08d89b1/smplx/body_models.py#L628) model. The path to the model can either be the path to the file with the parameters or a directory with the following structure:
```bash
models
├── smpl
│   ├── SMPL_FEMALE.pkl
│   └── SMPL_MALE.pkl
│   └── SMPL_NEUTRAL.pkl
├── smplh
│   ├── SMPLH_FEMALE.pkl
│   └── SMPLH_MALE.pkl
├── mano
|   ├── MANO_RIGHT.pkl
|   └── MANO_LEFT.pkl
└── smplx
    ├── SMPLX_FEMALE.npz
    ├── SMPLX_FEMALE.pkl
    ├── SMPLX_MALE.npz
    ├── SMPLX_MALE.pkl
    ├── SMPLX_NEUTRAL.npz
    └── SMPLX_NEUTRAL.pkl
```


## MANO and FLAME correspondences

The vertex correspondences between SMPL-X and MANO, FLAME can be downloaded
from [the project website](https://smpl-x.is.tue.mpg.de). If you have extracted
the correspondence data in the folder *correspondences*, then use the following
scripts to visualize them:

1. To view MANO correspondences run the following command:

```
python examples/vis_mano_vertices.py --model-folder $SMPLX_FOLDER --corr-fname correspondences/MANO_SMPLX_vertex_ids.pkl
```

2. To view FLAME correspondences run the following command:

```
python examples/vis_flame_vertices.py --model-folder $SMPLX_FOLDER --corr-fname correspondences/SMPL-X__FLAME_vertex_ids.npy
```

## Example

After installing the *smplx* package and downloading the model parameters you should be able to run the *demo.py*
script to visualize the results. For this step you have to install the [pyrender](https://pyrender.readthedocs.io/en/latest/index.html) and [trimesh](https://trimsh.org/) packages.

`python examples/demo.py --model-folder $SMPLX_FOLDER --plot-joints=True --gender="neutral"`

![SMPL-X Examples](https://github.com/vchoutas/smplx/raw/main/images/example.png)

## Modifying the global pose of the model

If you want to modify the global pose of the model, i.e. the root rotation and
translation, to a new coordinate system for example, you need to take into
account that the model rotation uses the pelvis as the center of rotation. A
more detailed description can be found in the following
[link](https://www.dropbox.com/scl/fi/zkatuv5shs8d4tlwr8ecc/Change-parameters-to-new-coordinate-system.paper?dl=0&rlkey=lotq1sh6wzkmyttisc05h0in0).
If something is not clear, please let me know so that I can update the
description.

## Citation

Depending on which model is loaded for your project, i.e. SMPL-X or SMPL+H or SMPL, please cite the most relevant work below, listed in the same order:

```
@inproceedings{SMPL-X:2019,
    title = {Expressive Body Capture: 3D Hands, Face, and Body from a Single Image},
    author = {Pavlakos, Georgios and Choutas, Vasileios and Ghorbani, Nima and Bolkart, Timo and Osman, Ahmed A. A. and Tzionas, Dimitrios and Black, Michael J.},
    booktitle = {Proceedings IEEE Conf. on Computer Vision and Pattern Recognition (CVPR)},
    year = {2019}
}
```

```
@article{MANO:SIGGRAPHASIA:2017,
    title = {Embodied Hands: Modeling and Capturing Hands and Bodies Together},
    author = {Romero, Javier and Tzionas, Dimitrios and Black, Michael J.},
    journal = {ACM Transactions on Graphics, (Proc. SIGGRAPH Asia)},
    volume = {36},
    number = {6},
    series = {245:1--245:17},
    month = nov,
    year = {2017},
    month_numeric = {11}
  }
```

```
@article{SMPL:2015,
    author = {Loper, Matthew and Mahmood, Naureen and Romero, Javier and Pons-Moll, Gerard and Black, Michael J.},
    title = {{SMPL}: A Skinned Multi-Person Linear Model},
    journal = {ACM Transactions on Graphics, (Proc. SIGGRAPH Asia)},
    month = oct,
    number = {6},
    pages = {248:1--248:16},
    publisher = {ACM},
    volume = {34},
    year = {2015}
}
```

This repository was originally developed for SMPL-X / SMPLify-X (CVPR 2019), you might be interested in having a look: [https://smpl-x.is.tue.mpg.de](https://smpl-x.is.tue.mpg.de).

## Acknowledgments

### Facial Contour

Special thanks to [Soubhik Sanyal](https://github.com/soubhiksanyal) for sharing the Tensorflow code used for the facial
landmarks.

## Contact
The code of this repository was implemented by [Vassilis Choutas](vassilis.choutas@tuebingen.mpg.de).

For questions, please contact [smplx@tue.mpg.de](smplx@tue.mpg.de).

For commercial licensing (and all related questions for business applications), please contact [ps-licensing@tue.mpg.de](ps-licensing@tue.mpg.de).

</div>
</details>

