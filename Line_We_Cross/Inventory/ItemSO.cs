using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;



#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

/// <summary>
/// 아이템의 데이터와 효과를 정의하는 스크립터블 오브젝트입니다.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    /// <summary>
    /// 아이템의 이미지 스프라이트입니다.
    /// </summary>
    [Title("아이템 이미지")]
    [HorizontalGroup("Header", 100)] // 가로 그룹 시작 (이미지 공간 80px)
    [VerticalGroup("Header/left")]
    [HideLabel] // 변수 이름 숨기기
    [PreviewField(80, ObjectFieldAlignment.Center)] // 80x80 사이즈로 이미지 미리보기
    public Sprite itemSprite;

    /// <summary>
    /// 게임 내 아이템의 프리팹을 나타냅니다.
    /// </summary>
    [Title("아이템 프리팹")]
    [VerticalGroup("Header/left")]
    [PreviewField(80, ObjectFieldAlignment.Center)]
    [HideLabel]
    public GameObject itemPrefab;

    /// <summary>
    /// 아이템의 이름을 가져오거나 설정합니다.
    /// </summary>
    [Title("아이템 정보")]
    [VerticalGroup("Header/Info")] // Header 그룹 내부에 세로 그룹 생성
    [LabelWidth(100)] // 레이블 넓이 조절
    public string itemName;

    /// <summary>
    /// 이 개체와 관련된 설명 텍스트를 가져오거나 설정합니다.
    /// </summary>
    [VerticalGroup("Header/Info")]
    [MultiLineProperty(3)] // 3줄 높이의 텍스트 입력창
    [LabelWidth(100)]
    public string description;

    /// <summary>
    /// 이 아이템 관련된 타입을 가져오거나 설정합니다.
    /// </summary>
    [VerticalGroup("Header/Info")]
    public ItemType itemType;

    /// <summary>
    /// 객체와 연관된 무게 값을 나타냅니다.
    /// </summary>
    [VerticalGroup("Header/Info")]
    [LabelWidth(100)]
    public float weight;

    /// <summary>
    /// 여러 개의 메시를 사용하고 있는지 여부를 나타내는 값을 가져오거나 설정합니다.
    /// </summary>
    [VerticalGroup("Header/Info")]
    public bool isMutipleMesh;

    /// <summary>
    /// 아이템과 연결된 메시 목록을 가져오거나 설정합니다.
    /// </summary>
    /// <remarks>이 속성은 항목에 대한 여러 메시를 저장하는 데 사용됩니다. 편집기에서 이 필드의 표시 여부는
    /// <c>ShowIf</c> 속성에 의해 제어되며, 이 속성은 <c>isMutipleMesh</c> 조건의 값에 따라 달라집니다.
    /// </remarks>
    [Title("추가 메시")]
    [HorizontalGroup("Lists")]
    [ShowIf("isMutipleMesh")]
    [SerializeField]
    public List<Mesh> itemMeshes = new List<Mesh>();

    [Title("추가 재질")]
    [ShowIf("isMutipleMesh")]
    [HorizontalGroup("Lists")]
    [SerializeField]
    public List<Material> itemMaterials = new List<Material>();


    [Title("아이템 효과 리스트")]
    public List<ItemTypeData> effectTypes = new List<ItemTypeData>();

    public List<ItemTypeData> GetEffects()
    {
        return effectTypes;
    }

    public enum ItemType 
    {
        Consumer, // 소비재
        Equipment, // 도구, 장비
        Construction, // 건설물
        Material, // 재료
    }

    // UNITY_EDITOR 내에서만 컴파일되도록 전처리기 지시문으로 감쌉니다.
#if UNITY_EDITOR

    #region 에셋 관리 기능

    [TitleGroup("에셋 관리", "이 에셋 파일을 관리합니다. 변경 사항은 즉시 파일에 적용됩니다.")]
    [BoxGroup("에셋 관리/이름 변경")]
    [HideLabel, SuffixLabel("새 파일 이름", true)]
    [InfoBox("변경할 새로운 파일 이름을 입력하세요. 확장자(.asset)는 자동으로 붙습니다.")]
    public string newAssetName;

    [BoxGroup("에셋 관리/이름 변경")]
    [Button("이름 변경", ButtonSizes.Medium)]
    private void RenameAsset()
    {
        // 새 이름이 비어있거나 현재 이름과 같은 경우 오류 메시지 표시
        if (string.IsNullOrEmpty(newAssetName) || newAssetName.Equals(this.name))
        {
            EditorUtility.DisplayDialog("오류", "새로운 파일 이름을 입력해주세요.", "확인");
            return;
        }

        string currentPath = AssetDatabase.GetAssetPath(this);
        string newPath = Path.Combine(Path.GetDirectoryName(currentPath), $"{newAssetName}.asset");

        // 이미 같은 이름의 에셋이 존재하는지 확인
        if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(newPath) != null)
        {
            EditorUtility.DisplayDialog("오류", "같은 이름의 에셋이 이미 존재합니다.", "확인");
            return;
        }

        // 에셋 이름 변경
        AssetDatabase.RenameAsset(currentPath, newAssetName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 입력 필드 초기화
        this.newAssetName = "";
        EditorUtility.DisplayDialog("완료", "에셋 이름이 성공적으로 변경되었습니다.", "확인");
    }


    [TitleGroup("에셋 관리")] // [BoxGroup("에셋 관리")] 에서 [TitleGroup("에셋 관리")] 로 수정하여 그룹 유형을 통일합니다.
    [Button("에셋 삭제", ButtonSizes.Large), GUIColor(0.9f, 0.3f, 0.3f)]
    private void DeleteAsset()
    {
        // 삭제 전 사용자에게 확인 받기
        if (EditorUtility.DisplayDialog("에셋 삭제 확인",
            $"정말로 '{this.name}' 에셋을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", "삭제", "취소"))
        {
            string path = AssetDatabase.GetAssetPath(this);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    #endregion

#endif
}
