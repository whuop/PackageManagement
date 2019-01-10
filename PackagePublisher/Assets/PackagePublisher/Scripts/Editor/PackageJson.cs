using UnityEngine;

namespace PackagePublisher.Json
{
    [System.Serializable]
    public class PackageJson
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public PackageAuthor author;
        [SerializeField]
        public PackageAuthor[] contributors;
        [SerializeField]
        public string displayName;
        [SerializeField]
        public string version;
        [SerializeField]
        public string unity;
        [SerializeField]
        public string description;
        [SerializeField]
        public string category;
        [SerializeField]
        public string[] keywords;
        [SerializeField]
        public PackageRepository repository;
    }
    
    [System.Serializable]
    public class PackageRepository
    {
        [SerializeField]
        public string type;
        [SerializeField]
        public string url;
    }
}

