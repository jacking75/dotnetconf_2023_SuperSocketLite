﻿using System;
using System.Collections.Generic;
using System.Linq;

using MemoryPack;


namespace MOServer;

public class Room
{
    public int Index { get; private set; }
    public int Number { get; private set; }

    int MaxUserCount = 0;

    List<RoomUser> UserList = new List<RoomUser>();

    public static Func<string, byte[], bool> NetSendFunc;


    public void Init(int index, int number, int maxUserCount)
    {
        Index = index;
        Number = number;
        MaxUserCount = maxUserCount;
    }

    public bool AddUser(string userID, string netSessionID)
    {
        if(GetUser(userID) != null)
        {
            return false;
        }

        var roomUser = new RoomUser();
        roomUser.Set(userID, netSessionID);
        UserList.Add(roomUser);

        return true;
    }

    public void RemoveUser(string netSessionID)
    {
        var index = UserList.FindIndex(x => x.NetSessionID == netSessionID);
        UserList.RemoveAt(index);
    }

    public bool RemoveUser(RoomUser user)
    {
        return UserList.Remove(user);
    }

    public RoomUser GetUser(string userID)
    {
        return UserList.Find(x => x.UserID == userID);
    }

    public RoomUser GetUserByNetSessionId(string netSessionID)
    {
        return UserList.Find(x => x.NetSessionID == netSessionID);
    }

    public int CurrentUserCount()
    {
        return UserList.Count();
    }

    public void NotifyPacketUserList(string userNetSessionID)
    {
        var packet = new PKTNtfRoomUserList();
        foreach (var user in UserList)
        {
            packet.UserIDList.Add(user.UserID);
        }

        var pktData = MemoryPackSerializer.Serialize(packet);
        PacketHeadReadWrite.Write(PACKETID.NTF_ROOM_USER_LIST, pktData);

        NetSendFunc(userNetSessionID, pktData);
    }

    public void NofifyPacketNewUser(string newUserNetSessionID, string newUserID)
    {
        var packet = new PKTNtfRoomNewUser();
        packet.UserID = newUserID;
        
        var pktData = MemoryPackSerializer.Serialize(packet);
        PacketHeadReadWrite.Write(PACKETID.NTF_ROOM_NEW_USER, pktData);

        Broadcast(newUserNetSessionID, pktData);
    }

    public void NotifyPacketLeaveUser(string userID)
    {
        if(CurrentUserCount() == 0)
        {
            return;
        }

        var packet = new PKTNtfRoomLeaveUser();
        packet.UserID = userID;
        
        var pktData = MemoryPackSerializer.Serialize(packet);
        PacketHeadReadWrite.Write(PACKETID.NTF_ROOM_LEAVE_USER, pktData);

        Broadcast("", pktData);
    }

    public void Broadcast(string excludeNetSessionID, byte[] sendPacket)
    {
        foreach(var user in UserList)
        {
            if(user.NetSessionID == excludeNetSessionID)
            {
                continue;
            }

            NetSendFunc(user.NetSessionID, sendPacket);
        }
    }
}


public class RoomUser
{
    public string UserID { get; private set; }
    public string NetSessionID { get; private set; }

    public void Set(string userID, string netSessionID)
    {
        UserID = userID;
        NetSessionID = netSessionID;
    }
}
