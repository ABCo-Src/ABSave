using ABCo.ABSave.Mapping.Description.Attributes;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ABCo.ABSave.Testing.ConsoleApp
{
    [MessagePackObject]
    [Serializable]
    [SaveMembers]
    public sealed class JsonResponseModel : IEquatable<JsonResponseModel>
    {
        public JsonResponseModel() { }

        public JsonResponseModel(bool initialize)
        {
            if (initialize)
            {
                Initialize();
            }
        }

        [Save(0)]
        public string Id { get; set; }

        [Save(1)]
        public string Type { get; set; }

        [Save(2)]
        public int Count { get; set; }

        [Save(3)]
        public DateTime CreationTime { get; set; }

        [Save(4)]
        public DateTime UpdateTime { get; set; }

        [Save(5)]
        public DateTime ExpirationTime { get; set; }

        [Save(6)]
        public string PreviousPageId { get; set; }

        [Save(7)]
        public string FollowingPageId { get; set; }

        [Save(8)]
        public List<ApiModelContainer> ModelContainers { get; set; }

        /// <inheritdoc/>
        public void Initialize()
        {
            Id = Randomizer.NextString(6);
            Type = nameof(JsonResponseModel);
            Count = Randomizer.NextInt();
            CreationTime = Randomizer.NextDateTime();
            UpdateTime = Randomizer.NextDateTime();
            ExpirationTime = Randomizer.NextDateTime();
            PreviousPageId = Randomizer.NextString(6);
            FollowingPageId = Randomizer.NextString(6);
            ModelContainers = new List<ApiModelContainer>();
            for (int i = 0; i < 50; i++)
            {
                var model = new ApiModelContainer();
                model.Initialize();
                ModelContainers.Add(model);
            }
        }

        /// <inheritdoc/>
        public bool Equals(JsonResponseModel other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                Id == other.Id &&
                Type == other.Type &&
                Count == other.Count &&
                CreationTime.Equals(other.CreationTime) &&
                UpdateTime.Equals(other.UpdateTime) &&
                ExpirationTime.Equals(other.ExpirationTime) &&
                PreviousPageId == other.PreviousPageId &&
                FollowingPageId == other.FollowingPageId &&
                ModelContainers?.Count == other.ModelContainers?.Count &&
                ModelContainers.Zip(other.ModelContainers).All(p => p.First.Equals(p.Second));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as JsonResponseModel);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [MessagePackObject]
    [Serializable]
    [SaveMembers]
    public sealed class ApiModelContainer : IEquatable<ApiModelContainer>
    {
        [Save(0)]
        public string Id { get; set; }

        [Save(1)]
        public string Type { get; set; }

        [Save(2)]
        public RestApiModel Model { get; set; }

        /// <inheritdoc/>
        public void Initialize()
        {
            Id = Randomizer.NextString(6);
            Type = nameof(JsonResponseModel);
            Model = new RestApiModel();
            Model.Initialize();
        }

        /// <inheritdoc/>
        public bool Equals(ApiModelContainer other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                throw new InvalidOperationException();
            }

            return
                Id?.Equals(other.Id) == true &&
                Type?.Equals(other.Type) == true &&
                Model?.Equals(other.Model) == true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ApiModelContainer);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [MessagePackObject]
    [Serializable]
    [SaveMembers]
    public sealed class RestApiModel : IEquatable<RestApiModel>
    {
        [Save(0)]
        public string Id { get; set; }

        [Save(1)]
        public string Type { get; set; }

        [Save(2)]
        public string Parent { get; set; }

        [Save(3)]
        public string Author { get; set; }

        [Save(4)]
        public string Title { get; set; }

        [Save(5)]
        public string Text { get; set; }

        [Save(6)]
        public string Url { get; set; }

        [Save(7)]
        public string HtmlContent { get; set; }

        [Save(8)]
        public int Upvotes { get; set; }

        [Save(9)]
        public int Downvotes { get; set; }

        [Save(10)]
        public float VotesRatio { get; set; }

        [Save(11)]
        public int Views { get; set; }

        [Save(12)]
        public int Clicks { get; set; }

        [Save(13)]
        public float ClicksRatio { get; set; }

        [Save(14)]
        public int NumberOfComments { get; set; }

        [Save(15)]
        public DateTime CreationTime { get; set; }

        [Save(16)]
        public DateTime UpdateTime { get; set; }

        [Save(17)]
        public DateTime ExpirationTime { get; set; }

        [Save(18)]
        public bool Flag1 { get; set; }

        [Save(19)]
        public bool Flag2 { get; set; }

        [Save(20)]
        public bool Flag3 { get; set; }

        [Save(21)]
        public bool Flag4 { get; set; }

        [Save(22)]
        public bool Flag5 { get; set; }

        [Save(23)]
        public string Optional1 { get; set; }

        [Save(24)]
        public string Optional2 { get; set; }

        [Save(25)]
        public string Optional3 { get; set; }

        [Save(26)]
        public MediaInfoModel Info { get; set; }

        /// <inheritdoc/>
        public void Initialize()
        {
            Id = Randomizer.NextString(6);
            Type = nameof(RestApiModel);
            Parent = Randomizer.NextString(6);
            Author = Randomizer.NextString(6);
            Title = Randomizer.NextString(Randomizer.NextInt(40, 120));
            if (Randomizer.NextBool())
            {

                Text = Randomizer.NextString(Randomizer.NextInt(80, 400));
                Url = null;
                HtmlContent = Randomizer.NextString(Randomizer.NextInt(100, 600));
            }
            else
            {
                Text = null;
                Url = Randomizer.NextString(Randomizer.NextInt(80, 120));
                HtmlContent = null;
            }
            Upvotes = Randomizer.NextInt();
            Downvotes = Randomizer.NextInt();
            VotesRatio = Upvotes / (float)Downvotes;
            Views = Randomizer.NextInt();
            Clicks = Randomizer.NextInt();
            ClicksRatio = Views / (float)Clicks;
            NumberOfComments = Randomizer.NextInt();
            CreationTime = Randomizer.NextDateTime();
            UpdateTime = Randomizer.NextDateTime();
            ExpirationTime = Randomizer.NextDateTime();
            Flag1 = Randomizer.NextBool();
            Flag2 = Randomizer.NextBool();
            Flag3 = Randomizer.NextBool();
            Flag4 = Randomizer.NextBool();
            Flag5 = Randomizer.NextBool();
            if (Randomizer.NextBool())
            {
                Optional1 = Randomizer.NextString(Randomizer.NextInt(6, 20));
            }

            if (Randomizer.NextBool())
            {
                Optional2 = Randomizer.NextString(Randomizer.NextInt(6, 20));
            }

            if (Randomizer.NextBool())
            {
                Optional3 = Randomizer.NextString(Randomizer.NextInt(6, 20));
            }

            if (Randomizer.NextBool())
            {
                Info = new MediaInfoModel();
                Info.Initialize();
            }
        }

        /// <inheritdoc/>
        public bool Equals(RestApiModel other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                Id == other.Id &&
                Type == other.Type &&
                Parent == other.Parent &&
                Author == other.Author &&
                Title == other.Title &&
                Text == other.Text &&
                Url == other.Url &&
                HtmlContent == other.HtmlContent &&
                Upvotes == other.Upvotes &&
                Downvotes == other.Downvotes &&
                MathF.Abs(VotesRatio - other.VotesRatio) < 0.001f &&
                Views == other.Views &&
                Clicks == other.Clicks &&
                MathF.Abs(ClicksRatio - other.ClicksRatio) < 0.001f &&
                NumberOfComments == other.NumberOfComments &&
                CreationTime.Equals(other.CreationTime) &&
                UpdateTime.Equals(other.UpdateTime) &&
                ExpirationTime.Equals(other.ExpirationTime) &&
                Flag1 == other.Flag1 &&
                Flag2 == other.Flag2 &&
                Flag3 == other.Flag3 &&
                Flag4 == other.Flag4 &&
                Flag5 == other.Flag5 &&
                Optional1 == other.Optional1 &&
                Optional2 == other.Optional2 &&
                Optional3 == other.Optional3 &&
                (Info == null && other.Info == null ||
                 Info?.Equals(other.Info) == true);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RestApiModel);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [MessagePackObject]
    [Serializable]
    [SaveMembers]
    public sealed class MediaInfoModel : IEquatable<MediaInfoModel>
    {
        [Save(0)]
        public string Id { get; set; }

        [Save(1)]
        public string AlbumUrl { get; set; }

        [Save(2)]
        public bool Property { get; set; }

        [Save(3)]
        public List<ImageModel> Images { get; set; }

        /// <inheritdoc/>
        public void Initialize()
        {
            Id = Randomizer.NextString(6);
            AlbumUrl = Randomizer.NextString(100);
            Property = Randomizer.NextBool();
            int count = Randomizer.NextInt() % 4 + 1;
            Images = new List<ImageModel>(count);
            for (int i = 0; i < count; i++)
            {
                var model = new ImageModel();
                model.Initialize();
                Images.Add(model);
            }
        }

        /// <inheritdoc/>
        public bool Equals(MediaInfoModel other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                Id?.Equals(other.Id) == true &&
                AlbumUrl?.Equals(other.AlbumUrl) == true &&
                Property == other.Property &&
                Images?.Count == other.Images?.Count &&
                Images.Zip(other.Images).All(p => p.First.Equals(p.Second));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MediaInfoModel);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [MessagePackObject]
    [Serializable]
    [SaveMembers]
    public sealed class ImageModel : IEquatable<ImageModel>
    {
        [Save(0)]
        public string Url { get; set; }

        [Save(1)]
        public int Width { get; set; }

        [Save(2)]
        public int Height { get; set; }

        [Save(3)]
        public float AspectRatio { get; set; }

        /// <inheritdoc/>
        public void Initialize()
        {
            Url = Randomizer.NextString(Randomizer.NextInt(140, 200));
            Width = Randomizer.NextInt();
            Height = Randomizer.NextInt();
            AspectRatio = Width / (float)Height;
        }

        /// <inheritdoc/>
        public bool Equals(ImageModel other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                throw new InvalidOperationException();
            }

            return
                Url?.Equals(other.Url) == true &&
                Width == other.Width &&
                Height == other.Height &&
                MathF.Abs(AspectRatio - other.AspectRatio) < 0.001f;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ImageModel);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    static class Randomizer
    {
        private static readonly Random Random = new Random();
        public static bool NextBool() => Random.Next() % 2 == 1;
        public static int NextInt() => Random.Next();
        public static int NextInt(int min, int max) => Random.Next(min, max);
        public static double NextDouble() => Random.NextDouble();
        public static DateTime NextDateTime() => DateTime.Today.AddSeconds(Random.Next(0, 31536000)).ToUniversalTime();

        public static string NextString(int length) => string.Create(length, Random, (chars, r) =>
        {
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)r.Next(65, 90);
            }
        });
    }
}
