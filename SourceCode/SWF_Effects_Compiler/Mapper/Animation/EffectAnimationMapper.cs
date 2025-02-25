using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json.Serialization;

namespace Habbo_Downloader.SWF_Effects_Compiler.Mapper.Animation
{
    // Data models for animations.
    public class AssetAnimation
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("resetOnToggle")]
        public bool? ResetOnToggle { get; set; }

        [JsonPropertyName("directions")]
        public List<AssetAnimationDirection>? Directions { get; set; }

        [JsonPropertyName("shadows")]
        public List<AssetAnimationShadow>? Shadows { get; set; }

        [JsonPropertyName("adds")]
        public List<AssetAnimationAdd>? Adds { get; set; }

        [JsonPropertyName("removes")]
        public List<AssetAnimationRemove>? Removes { get; set; }

        [JsonPropertyName("sprites")]
        public List<AssetAnimationSprite>? Sprites { get; set; }

        [JsonPropertyName("frames")]
        public List<AssetAnimationFrame>? Frames { get; set; }

        [JsonPropertyName("avatars")]
        public List<AssetAnimationAvatar>? Avatars { get; set; }

        [JsonPropertyName("overrides")]
        public List<AssetAnimationOverride>? Overrides { get; set; }
    }

    public class AssetAnimationDirection
    {
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }
    }

    public class AssetAnimationRemove
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class AssetAnimationAdd
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("align")]
        public string? Align { get; set; }

        [JsonPropertyName("blend")]
        public string? Blend { get; set; }

        [JsonPropertyName("ink")]
        public int? Ink { get; set; }
        
        [JsonPropertyName("base")]
        public string? Base { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }

    public class AssetAnimationShadow
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class AssetAnimationSprite
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("directions")]
        public int? Directions { get; set; }

        [JsonPropertyName("member")]
        public string? Member { get; set; }

        [JsonPropertyName("ink")]
        public int? Ink { get; set; }        

        [JsonPropertyName("staticY")]
        public int? StaticY { get; set; }

        [JsonPropertyName("directionList")]
        public List<AssetAnimationSpriteDirection>? DirectionList { get; set; }
    }

    public class AssetAnimationSpriteDirection
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("dx")]
        public int? Dx { get; set; }

        [JsonPropertyName("dy")]
        public int? Dy { get; set; }

        [JsonPropertyName("dz")]
        public int? Dz { get; set; }
    }

    public class AssetAnimationFrame
    {
        [JsonPropertyName("repeats")]
        public int? Repeats { get; set; }

        [JsonPropertyName("fxs")]
        public List<AssetAnimationFramePart>? Fxs { get; set; }

        [JsonPropertyName("bodyparts")]
        public List<AssetAnimationFramePart>? Bodyparts { get; set; }
    }

    public class AssetAnimationFramePart
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("frame")]
        public int? Frame { get; set; }

        [JsonPropertyName("dx")]
        public int? Dx { get; set; }

        [JsonPropertyName("dy")]
        public int? Dy { get; set; }

        [JsonPropertyName("dz")]
        public int? Dz { get; set; }

        [JsonPropertyName("dd")]
        public int? Dd { get; set; }

        // Set order so "base" comes before "action"
        [JsonPropertyName("base")]
        [JsonPropertyOrder(1)]
        public string? Base { get; set; }

        [JsonPropertyName("action")]
        [JsonPropertyOrder(2)]
        public string? Action { get; set; }

        [JsonPropertyName("items")]
        public List<AssetAnimationFramePartItem>? Items { get; set; }
    }

    public class AssetAnimationFramePartItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("base")]
        public string? Base { get; set; }
    }

    public class AssetAnimationAvatar
    {
        [JsonPropertyName("background")]
        [JsonPropertyOrder(1)]
        public string? Background { get; set; }

        [JsonPropertyName("foreground")]
        [JsonPropertyOrder(2)]
        public string? Foreground { get; set; }

        [JsonPropertyName("ink")]
        [JsonPropertyOrder(3)]
        public int? Ink { get; set; }
    }

    public class AssetAnimationOverride
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("override")]
        public string? Override { get; set; }

        [JsonPropertyName("frames")]
        public List<AssetAnimationFrame>? Frames { get; set; }
    }

    public static class EffectAnimationMapper
    {
        public static async Task<Dictionary<string, AssetAnimation>> ParseAnimationFileAsync(string binaryOutputPath)
        {
            // Look for the *_animation.bin file (adjust pattern if needed)
            string[] animationFiles = Directory.GetFiles(binaryOutputPath, "*_animation.bin", SearchOption.TopDirectoryOnly);
            if (animationFiles.Length == 0)
            {
                Console.WriteLine($"❌ Animation file not found in {binaryOutputPath}");
                return new Dictionary<string, AssetAnimation>();
            }

            string animationFilePath = animationFiles[0];
            string animationContent = await File.ReadAllTextAsync(animationFilePath);
            XElement animationXml;
            try
            {
                animationXml = XElement.Parse(animationContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parsing animation file {animationFilePath}: {ex.Message}");
                return new Dictionary<string, AssetAnimation>();
            }

            var animations = new Dictionary<string, AssetAnimation>();
            MapAnimationXML(animationXml, animations);
            return animations;
        }

        private static void MapAnimationXML(XElement xml, Dictionary<string, AssetAnimation> output)
        {
            if (xml == null || output == null)
                return;

            // Create an animation instance from attributes.
            var animation = new AssetAnimation
            {
                Name = xml.Attribute("name")?.Value,
                Desc = xml.Attribute("desc")?.Value
            };

            if (bool.TryParse(xml.Attribute("resetOnToggle")?.Value, out bool reset))
                animation.ResetOnToggle = reset;

            // Iterate over each direct child element.
            foreach (var element in xml.Elements())
            {
                switch (element.Name.LocalName.ToLower())
                {
                    case "direction":
                        if (animation.Directions == null)
                            animation.Directions = new List<AssetAnimationDirection>();
                        if (int.TryParse(element.Attribute("offset")?.Value, out int offset))
                            animation.Directions.Add(new AssetAnimationDirection { Offset = offset });
                        break;

                    case "shadow":
                        if (animation.Shadows == null)
                            animation.Shadows = new List<AssetAnimationShadow>();
                        animation.Shadows.Add(new AssetAnimationShadow
                        {
                            Id = element.Attribute("id")?.Value
                        });
                        break;

                    case "adds":
                    case "add":
                        if (animation.Adds == null)
                            animation.Adds = new List<AssetAnimationAdd>();
                        if (element.Name.LocalName.ToLower() == "adds")
                        {
                            foreach (var addElement in element.Elements("add"))
                            {
                                var add = new AssetAnimationAdd
                                {
                                    Id = addElement.Attribute("id")?.Value,
                                    Align = addElement.Attribute("align")?.Value,
                                    Blend = addElement.Attribute("blend")?.Value,
                                    Base = addElement.Attribute("base")?.Value,
                                    Action = addElement.Attribute("action")?.Value
                                };
                                if (int.TryParse(addElement.Attribute("ink")?.Value, out int addInk))
                                    add.Ink = addInk;
                                animation.Adds.Add(add);
                            }
                        }
                        else
                        {
                            var add = new AssetAnimationAdd
                            {
                                Id = element.Attribute("id")?.Value,
                                Align = element.Attribute("align")?.Value,
                                Blend = element.Attribute("blend")?.Value,
                                Base = element.Attribute("base")?.Value,
                                Action = element.Attribute("action")?.Value
                            };
                            if (int.TryParse(element.Attribute("ink")?.Value, out int addInk))
                                add.Ink = addInk;
                            animation.Adds.Add(add);
                        }
                        break;


                    case "remove":
                        if (animation.Removes == null)
                            animation.Removes = new List<AssetAnimationRemove>();
                        animation.Removes.Add(new AssetAnimationRemove
                        {
                            Id = element.Attribute("id")?.Value
                        });
                        break;

                    case "avatar":
                        if (animation.Avatars == null)
                            animation.Avatars = new List<AssetAnimationAvatar>();
                        var avatar = new AssetAnimationAvatar();
                        if (int.TryParse(element.Attribute("ink")?.Value, out int avatarInk))
                            avatar.Ink = avatarInk;
                        avatar.Foreground = element.Attribute("foreground")?.Value;
                        avatar.Background = element.Attribute("background")?.Value;
                        animation.Avatars.Add(avatar);
                        break;

                    case "sprite":
                        if (animation.Sprites == null)
                            animation.Sprites = new List<AssetAnimationSprite>();
                        var sprite = new AssetAnimationSprite
                        {
                            Id = element.Attribute("id")?.Value,
                            Member = element.Attribute("member")?.Value
                        };
                        if (int.TryParse(element.Attribute("ink")?.Value, out int spriteInk))
                            sprite.Ink = spriteInk;
                        if (int.TryParse(element.Attribute("staticY")?.Value, out int staticY))
                            sprite.StaticY = staticY;
                        if (int.TryParse(element.Attribute("directions")?.Value, out int directionsCount))
                            sprite.Directions = directionsCount;
                        var spriteDirections = new List<AssetAnimationSpriteDirection>();
                        foreach (var d in element.Elements("direction"))
                        {
                            var spriteDir = new AssetAnimationSpriteDirection();
                            if (int.TryParse(d.Attribute("id")?.Value, out int id))
                                spriteDir.Id = id;
                            if (int.TryParse(d.Attribute("dx")?.Value, out int dx))
                                spriteDir.Dx = dx;
                            if (int.TryParse(d.Attribute("dy")?.Value, out int dy))
                                spriteDir.Dy = dy;
                            if (int.TryParse(d.Attribute("dz")?.Value, out int dz))
                                spriteDir.Dz = dz;
                            spriteDirections.Add(spriteDir);
                        }
                        if (spriteDirections.Count > 0)
                            sprite.DirectionList = spriteDirections;
                        animation.Sprites.Add(sprite);
                        break;


                    case "frame":
                        if (animation.Frames == null)
                            animation.Frames = new List<AssetAnimationFrame>();
                        var frame = MapFrame(element);
                        if (frame != null)
                            animation.Frames.Add(frame);
                        break;

                    case "override":
                        if (animation.Overrides == null)
                            animation.Overrides = new List<AssetAnimationOverride>();
                        var ov = new AssetAnimationOverride
                        {
                            Name = element.Attribute("name")?.Value,
                            Override = element.Attribute("override")?.Value
                        };
                        var overrideFramesElement = element.Element("frames");
                        if (overrideFramesElement != null)
                        {
                            ov.Frames = MapOverrideFrames(overrideFramesElement);
                        }
                        else
                        {
                            var ovFrames = new List<AssetAnimationFrame>();
                            foreach (var frameEl in element.Elements("frame"))
                            {
                                var ovFrame = MapFrame(frameEl);
                                if (ovFrame != null)
                                    ovFrames.Add(ovFrame);
                            }
                            if (ovFrames.Count > 0)
                                ov.Frames = ovFrames;
                        }
                        animation.Overrides.Add(ov);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(animation.Desc))
                output[animation.Desc] = animation;
        }

        private static AssetAnimationFrame? MapFrame(XElement frameEl)
        {
            if (frameEl == null)
                return null;
            var frame = new AssetAnimationFrame();

            // Map "repeats" attribute if present.
            if (int.TryParse(frameEl.Attribute("repeats")?.Value, out int repeats))
                frame.Repeats = repeats;

            foreach (var child in frameEl.Elements())
            {
                if (child.Name.LocalName.ToLower() == "fx")
                {
                    if (frame.Fxs == null)
                        frame.Fxs = new List<AssetAnimationFramePart>();
                    var fx = new AssetAnimationFramePart
                    {
                        Id = child.Attribute("id")?.Value,
                        Action = child.Attribute("action")?.Value
                    };
                    if (int.TryParse(child.Attribute("frame")?.Value, out int fxFrame))
                        fx.Frame = fxFrame;
                    if (int.TryParse(child.Attribute("dx")?.Value, out int dx))
                        fx.Dx = dx;
                    if (int.TryParse(child.Attribute("dy")?.Value, out int dy))
                        fx.Dy = dy;
                    if (int.TryParse(child.Attribute("dz")?.Value, out int dz))
                        fx.Dz = dz;
                    // New: parse dd attribute for fx elements.
                    if (int.TryParse(child.Attribute("dd")?.Value, out int dd))
                        fx.Dd = dd;
                    frame.Fxs.Add(fx);
                }
                else if (child.Name.LocalName.ToLower() == "bodypart")
                {
                    if (frame.Bodyparts == null)
                        frame.Bodyparts = new List<AssetAnimationFramePart>();
                    var bp = new AssetAnimationFramePart
                    {
                        Id = child.Attribute("id")?.Value,
                        Action = child.Attribute("action")?.Value
                    };
                    if (int.TryParse(child.Attribute("frame")?.Value, out int bpFrame))
                        bp.Frame = bpFrame;
                    if (int.TryParse(child.Attribute("dx")?.Value, out int bdx))
                        bp.Dx = bdx;
                    if (int.TryParse(child.Attribute("dy")?.Value, out int bdy))
                        bp.Dy = bdy;
                    if (int.TryParse(child.Attribute("dd")?.Value, out int bdd))
                        bp.Dd = bdd;
                    bp.Base = child.Attribute("base")?.Value;

                    var items = new List<AssetAnimationFramePartItem>();
                    foreach (var itemEl in child.Elements("item"))
                    {
                        var item = new AssetAnimationFramePartItem
                        {
                            Id = itemEl.Attribute("id")?.Value,
                            Base = itemEl.Attribute("base")?.Value
                        };
                        items.Add(item);
                    }
                    if (items.Count > 0)
                        bp.Items = items;
                    frame.Bodyparts.Add(bp);
                }
            }
            return frame;
        }


        private static List<AssetAnimationFrame> MapOverrideFrames(XElement framesElement)
        {
            var frames = new List<AssetAnimationFrame>();
            foreach (var frameEl in framesElement.Elements("frame"))
            {
                var frame = MapFrame(frameEl);
                if (frame != null)
                    frames.Add(frame);
            }
            return frames;
        }
    }
}
