using System.Collections;
using System.Collections.Generic;

public class ResultObject { }


namespace ImageClassification{
	[System.Serializable]
	public class DjangoResult : ResultObject {
		// Django ImageClassification Server Response 전용 오브젝트.
		public ResultElement[] result;

		/// <summary>
		/// 결과 ID들을 참조시킬 수 있는 메소드.
		/// </summary>
		public int[] GetAllId()
		{
			List<int> list = new List<int>();
			foreach(ResultElement e in result)
			{
				list.Add(e.id);
			}
			return list.ToArray();
		}
	}

	[System.Serializable]
	public class ResultElement {
			public int id;
			public float percentage;

	}


	[System.Serializable]
	public class NodeResult : ResultObject {
		// Node ImageClassification Server Response 전용 오브젝트.
		public InfoElement[] result;
	}

	[System.Serializable]
	public class InfoElement {
			public int _id;
			public float avgScore;
			public string name;
			public string imageUrl;
			public string category;
	}
}

namespace ImageRegistration{
	[System.Serializable]
	public class DjangoResult : ResultObject {
		// Django ImageRegistration Server Response 전용 오브젝트.
		
	}

	[System.Serializable]
	public class NodeResult : ResultObject {
		// Node ImageRegistration Server Response 전용 오브젝트.
		public InfoElement result;
	}

	[System.Serializable]
	public class InfoElement {
		public int id;

	}
}

namespace SearchText
{

    [System.Serializable]
    public class NodeResult : ResultObject
    {
        // Node ImageRegistration Server Response 전용 오브젝트.
		public InfoElement[] result;

	}
	[System.Serializable]
    public class InfoElement
	{
		public int _id;
		public string name;
		public string category;
		public string destName;

	}
}

namespace GoogleDrive
{
	[System.Serializable]
	public class GoogleDriveResult : ResultObject
	{
		public string kind;
		public string nextPageToken;
		public bool incompleteSearch;
		public List<GoogleFiles> files;

		/// <summary>
		/// 검색한 제품 ID의 구글 드라이브 폴더 id 반환 혹은 검색한 파일 id 반환.
		/// </summary>
		public string GetFolderId()
		{
			return files[0].id;
		}

		/// <summary>
		/// 검색조건에 따른 파일 갯수 반환.
		/// </summary>
		public int GetFilesListSize()
		{
			return files.Count;
		}
	}

	[System.Serializable]
	public class GoogleFiles : ResultObject
	{
		public string kind;
		public string id;
		public string name;
		public string mimeType;
	}

	[System.Serializable]
	public class RefreshTokenResult : ResultObject
	{
		public string access_token;
		public int expires_in;
		public string scope;
		public string token_type;

		/// <summary>
		/// 새로운 Token 재발급 함수.
		/// </summary>
		public string GetRefreshToken()
		{
			return token_type + " " + access_token;
		}
	}
}