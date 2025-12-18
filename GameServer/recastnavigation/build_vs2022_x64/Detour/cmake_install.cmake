# Install script for directory: C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "C:/Program Files/RecastNavigation")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Release")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "FALSE")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/build_vs2022_x64/Detour/Debug/Detour-d.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/build_vs2022_x64/Detour/Release/Detour.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/build_vs2022_x64/Detour/MinSizeRel/Detour.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/build_vs2022_x64/Detour/RelWithDebInfo/Detour.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/recastnavigation" TYPE FILE FILES
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourAlloc.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourAssert.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourCommon.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourMath.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourNavMesh.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourNavMeshBuilder.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourNavMeshQuery.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourNode.h"
    "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/Detour/Include/DetourStatus.h"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE FILE OPTIONAL FILES "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/build_vs2022_x64/Detour/Debug/Detour-d.pdb")
  endif()
endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "C:/Users/hyeon-308-9/Network_IOCP_FianlProject/GameServer/recastnavigation/build_vs2022_x64/Detour/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
