// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osu.Game.Rulesets.Judgements;
using osu.Game.Screens.Play.HUD;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Skinning
{
    public class LegacyHealthDisplay : CompositeDrawable, IHealthDisplay
    {
        private readonly Skin skin;
        private LegacyHealthPiece fill;
        private LegacyHealthPiece marker;

        private float maxFillWidth;

        private bool isNewStyle;

        public Bindable<double> Current { get; } = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        public LegacyHealthDisplay(Skin skin)
        {
            this.skin = skin;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            isNewStyle = getTexture(skin, "marker") != null;

            // background implementation is the same for both versions.
            AddInternal(new Sprite { Texture = getTexture(skin, "bg") });

            if (isNewStyle)
            {
                AddRangeInternal(new[]
                {
                    fill = new LegacyNewStyleFill(skin),
                    marker = new LegacyNewStyleMarker(skin),
                });
            }
            else
            {
                AddRangeInternal(new[]
                {
                    fill = new LegacyOldStyleFill(skin),
                    marker = new LegacyOldStyleMarker(skin),
                });
            }

            fill.Current.BindTo(Current);
            marker.Current.BindTo(Current);

            maxFillWidth = fill.Width;
        }

        protected override void Update()
        {
            base.Update();

            fill.Width = Interpolation.ValueAt(
                Math.Clamp(Clock.ElapsedFrameTime, 0, 200),
                fill.Width, (float)Current.Value * maxFillWidth, 0, 200, Easing.OutQuint);

            marker.Position = fill.Position + new Vector2(fill.DrawWidth, fill.DrawHeight / 2);
        }

        public void Flash(JudgementResult result) => marker.Flash(result);

        private static Texture getTexture(Skin skin, string name) => skin.GetTexture($"scorebar-{name}");

        private static Color4 getFillColour(double hp)
        {
            if (hp < 0.2)
                return Interpolation.ValueAt(0.2 - hp, Color4.Black, Color4.Red, 0, 0.2);

            if (hp < 0.5)
                return Interpolation.ValueAt(0.5 - hp, Color4.White, Color4.Black, 0, 0.5);

            return Color4.White;
        }

        public class LegacyOldStyleMarker : LegacyHealthPiece
        {
            private readonly Sprite sprite;

            private readonly Texture normalTexture;
            private readonly Texture dangerTexture;
            private readonly Texture superDangerTexture;

            public LegacyOldStyleMarker(Skin skin)
            {
                Origin = Anchor.Centre;

                normalTexture = getTexture(skin, "ki");
                dangerTexture = getTexture(skin, "kidanger");
                superDangerTexture = getTexture(skin, "kidanger2");

                InternalChildren = new Drawable[]
                {
                    sprite = new Sprite
                    {
                        Texture = getTexture(skin, "ki"),
                        Origin = Anchor.Centre,
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(hp =>
                {
                    if (hp.NewValue < 0.2f)
                        sprite.Texture = superDangerTexture;
                    else if (hp.NewValue < 0.5f)
                        sprite.Texture = dangerTexture;
                    else
                        sprite.Texture = normalTexture;
                });
            }

            public override void Flash(JudgementResult result)
            {
                this.ScaleTo(1.4f).Then().ScaleTo(1, 200, Easing.Out);
            }
        }

        public class LegacyNewStyleMarker : LegacyHealthPiece
        {
            private readonly Sprite sprite;

            public LegacyNewStyleMarker(Skin skin)
            {
                Origin = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    sprite = new Sprite
                    {
                        Texture = getTexture(skin, "marker"),
                        Origin = Anchor.Centre,
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                sprite.Colour = getFillColour(Current.Value);
                sprite.Blending = Current.Value < 0.5f ? BlendingParameters.Inherit : BlendingParameters.Additive;
            }

            public override void Flash(JudgementResult result)
            {
                this.ScaleTo(1.4f).Then().ScaleTo(1, 200, Easing.Out);
            }
        }

        internal class LegacyOldStyleFill : LegacyHealthPiece
        {
            public LegacyOldStyleFill(Skin skin)
            {
                // required for sizing correctly..
                var firstFrame = getTexture(skin, "colour-0");

                if (firstFrame == null)
                {
                    InternalChild = new Sprite { Texture = getTexture(skin, "colour") };
                    Size = InternalChild.Size;
                }
                else
                {
                    InternalChild = skin.GetAnimation("scorebar-colour", true, true, startAtCurrentTime: false, applyConfigFrameRate: true) ?? Drawable.Empty();
                    Size = new Vector2(firstFrame.DisplayWidth, firstFrame.DisplayHeight);
                }

                Position = new Vector2(3, 10) * 1.6f;
                Masking = true;
            }
        }

        internal class LegacyNewStyleFill : LegacyHealthPiece
        {
            public LegacyNewStyleFill(Skin skin)
            {
                InternalChild = new Sprite
                {
                    Texture = getTexture(skin, "colour"),
                };

                Size = InternalChild.Size;
                Position = new Vector2(7.5f, 7.8f) * 1.6f;
                Masking = true;
            }

            protected override void Update()
            {
                base.Update();
                this.Colour = getFillColour(Current.Value);
            }
        }

        public class LegacyHealthPiece : CompositeDrawable, IHealthDisplay
        {
            public Bindable<double> Current { get; } = new Bindable<double>();

            public virtual void Flash(JudgementResult result)
            {
            }
        }
    }
}
