CSC = $(MCS) -codepage:utf8 -target:library $(MCS_FLAGS)

ASSEMBLY = PasswordAttributeViewer

CSFILES = \
	PasswordAttributeViewer.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

if BUILD_AVAHI
AVAHI_REFERENCES = $(AVAHI_LIBS)
endif

if BUILD_NETWORKMANAGER
NM_REFERENCES = \
        $(top_builddir)/network-manager/network-manager.dll
endif

REFERENCES = \
	$(NM_REFERENCES) \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

$(ASSEMBLY).dll: $(SOURCES_BUILD)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(REFERENCES_BUILD) $(AVAHI_REFERENCES) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
