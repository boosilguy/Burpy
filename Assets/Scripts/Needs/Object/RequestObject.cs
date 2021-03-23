using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestObject{  }

namespace ImageClassification
{
    public class DjangoRequest : RequestObject
    {
        // Django Server Request 전용 오브젝트
        public string image; // {"image":"value"}

        public DjangoRequest(string str_image)
        {
            this.image = str_image;
        }
    }


    public class NodeRequest : RequestObject
    {
        // Node Server Request 전용 오브젝트
        public List<int> list; // {"list":[1, 2, 3, 4,5...12]}

        public NodeRequest(int[] num_array)
        {
            list = new List<int>(num_array);
        }
    }

}

namespace ImageRegistration
{
    public class NodeRequest : RequestObject
    {
        // Node Server Request 전용 오브젝트
        public string name;
        public string category;
        
        public NodeRequest(string name, string category)
        {
            this.name = name;
            this.category = category;
        }
    }
}

namespace SearchText
{
    public class NodeRequest : RequestObject
    {
        // Node SearchText Server Response 전용 오브젝트.
        public string name;

        public NodeRequest(string name)
        {
            this.name = name;
        }
    }
}