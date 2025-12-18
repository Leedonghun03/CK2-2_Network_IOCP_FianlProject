cmake_minimum_required(VERSION 3.20)
project(gameserver LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

file(GLOB_RECURSE SRC CONFIGURE_DEPENDS
    "*.cpp"
    "*.h"
)

add_executable(gameserver ${SRC})