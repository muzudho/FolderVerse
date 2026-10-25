namespace FolderVerse;
public static class NodeSupervisors
{
    public static readonly string[] Kinds={"construction","mayor","town","village","harbor","captain","admiral","pirate","polar","abbess","hospital","class","nursery","shop"};
    public static readonly string[] Labels={"工事監督","市長","町長","村長","港湾監督","船長","艦長","海賊","極地探検隊長","修道院長","病院長","学級委員長","保母","店長"};
    public static int For(Node node,int face)
    {
        if(face is 2 or 3)return 8;
        if(node.IsHarbor)return 4;
        if(node.Terrain==TravelTerrain.Sea)return 5+node.Id%3;
        if(node.Terrain==TravelTerrain.Mountain)return 0;
        // Facility roles are provisional land-node flavour until named facilities are introduced.
        int role=node.Id%10;if(role>=5)return 9+role-5;
        return role==0?0:node.Population.Total>=1000?1:node.Population.Total>=300?2:3;
    }
}
